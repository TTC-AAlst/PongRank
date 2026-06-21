using Microsoft.Extensions.Logging.Abstractions;
using PongRank.DataAccess;
using PongRank.DataEntities;
using PongRank.Model;
using PongRank.Tests.Fakes;
using PongRank.WebApi.Utilities;
using Xunit;

namespace PongRank.Tests;

public class HistoricalSyncRunnerTests
{
    // Fixed "now" → CurrentYear = 2025 (June < September → year - 1).
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero));

    private static SyncJobSettings Settings(params int[] years) => new()
    {
        SyncYears = years,
        SyncCompetitions = new[] { Competition.Vttl },
        SyncCategoryNames = new[] { "Oost-Vlaanderen" },
    };

    private static HistoricalSyncRunner Runner(TtcDbContext db, FakeFrenoyApiClient frenoy, SyncJobSettings settings, INtfyNotifier? notifier = null) =>
        new(db, frenoy, settings, Clock, NullLogger<HistoricalSyncRunner>.Instance, notifier ?? new FakeNtfyNotifier());

    [Fact]
    public async Task Closed_year_is_skipped_no_api_call()
    {
        using var db = InMemoryDb.Create();
        db.SyncStates.Add(new SyncStateEntity { Competition = Competition.Vttl, Year = 2020, Status = SyncStatus.Closed });
        await db.SaveChangesAsync();
        var frenoy = new FakeFrenoyApiClient();

        await Runner(db, frenoy, Settings(2020)).RunAsync();

        Assert.Empty(frenoy.SyncedYears);
    }

    [Fact]
    public async Task PastYear_closes_after_clean_pass()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient();

        await Runner(db, frenoy, Settings(2024)).RunAsync();

        var state = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Closed, state.Status);
        Assert.Equal(1, state.AttemptCount);
        Assert.Equal("Completed", state.LastOutcome);
        Assert.Single(frenoy.SyncedYears);
    }

    [Fact]
    public async Task CurrentYear_stays_pending()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient();

        await Runner(db, frenoy, Settings(2025)).RunAsync();

        var state = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Pending, state.Status);
        Assert.Equal("Completed", state.LastOutcome);
        Assert.Equal(1, state.AttemptCount);
    }

    [Fact]
    public async Task QuotaExceeded_keeps_year_pending_and_stops_the_run()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient { ThrowOnSync = new Exception("Quota exceeded for today") };

        await Runner(db, frenoy, Settings(2023, 2022)).RunAsync();

        // Only the first year was attempted; the run stopped on quota.
        Assert.Single(frenoy.SyncedYears);
        var state = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Pending, state.Status);
        Assert.Equal("QuotaExceeded", state.LastOutcome);
    }

    [Fact]
    public async Task QuotaExceeded_logs_the_server_message()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient { ThrowOnSync = new Exception("Quota exceeded for today") };
        var logger = new CapturingLogger<HistoricalSyncRunner>();

        await new HistoricalSyncRunner(db, frenoy, Settings(2023), Clock, logger, new FakeNtfyNotifier()).RunAsync();

        // The TabT server's own quota message must survive to the log — the dashboard
        // gauge (current=0) is unreliable, so this is the only authoritative signal.
        Assert.Contains(logger.Messages,
            m => m.Contains("outcome=QuotaExceeded") && m.Contains("Quota exceeded for today"));
    }

    [Fact]
    public async Task NonQuota_error_records_Error_and_continues_to_next_year()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient
        {
            OnSync = s => s.Year == 2023 ? throw new Exception("boom (not quota)") : Task.CompletedTask,
        };

        await Runner(db, frenoy, Settings(2023, 2022)).RunAsync();

        // Both years attempted — a non-quota error does NOT stop the run.
        Assert.Equal(2, frenoy.SyncedYears.Count);
        var failed = Assert.Single(db.SyncStates, x => x.Year == 2023);
        Assert.Equal("Error", failed.LastOutcome);
        Assert.Equal(SyncStatus.Pending, failed.Status); // Status preserved on error
        var ok = Assert.Single(db.SyncStates, x => x.Year == 2022);
        Assert.Equal(SyncStatus.Closed, ok.Status);
    }

    [Fact]
    public async Task PastYear_with_incomplete_club_closes_incomplete()
    {
        using var db = InMemoryDb.Create();
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Oost-Vlaanderen", SyncCompleted = false });
        await db.SaveChangesAsync();
        var frenoy = new FakeFrenoyApiClient();

        await Runner(db, frenoy, Settings(2024)).RunAsync();

        var state = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Closed, state.Status);
        Assert.Equal("Closed-incomplete", state.LastOutcome);
    }

    [Fact]
    public async Task NothingLeft_ignores_clubs_outside_SyncCategoryNames()
    {
        using var db = InMemoryDb.Create();
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Oost-Vlaanderen", SyncCompleted = true });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Antwerpen", SyncCompleted = false });
        await db.SaveChangesAsync();
        var frenoy = new FakeFrenoyApiClient();

        await Runner(db, frenoy, Settings(2024)).RunAsync();

        var state = Assert.Single(db.SyncStates);
        Assert.Equal("Completed", state.LastOutcome); // the out-of-category incomplete club is ignored
    }

    [Fact]
    public async Task Quota_on_first_competition_stops_before_second()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient { ThrowOnSync = new Exception("Quota exceeded for today") };
        var settings = new SyncJobSettings
        {
            SyncYears = new[] { 2024 },
            SyncCompetitions = new[] { Competition.Vttl, Competition.Sporta },
            SyncCategoryNames = new[] { "Oost-Vlaanderen" },
        };

        await Runner(db, frenoy, settings).RunAsync();

        // Quota on Vttl aborts the whole run — Sporta is never attempted.
        Assert.Single(frenoy.SyncedYears);
        Assert.Equal("Vttl", frenoy.SyncedYears[0].Competition);
    }

    [Fact]
    public async Task New_matches_trigger_one_slack_notification()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient
        {
            OnSync = async _ =>
            {
                db.Matches.Add(new MatchEntity { Competition = Competition.Vttl, Year = 2024 });
                await db.SaveChangesAsync();
            },
        };
        var ntfy = new FakeNtfyNotifier();

        await Runner(db, frenoy, Settings(2024), ntfy).RunAsync();

        var sent = Assert.Single(ntfy.Sent);
        Assert.Equal(Competition.Vttl, sent.Competition);
        Assert.Equal(2024, sent.Year);
        Assert.Equal(1, sent.NewMatches);
    }

    [Fact]
    public async Task No_new_matches_sends_no_notification()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient();
        var ntfy = new FakeNtfyNotifier();

        await Runner(db, frenoy, Settings(2024), ntfy).RunAsync();

        Assert.Empty(ntfy.Sent);
    }

    [Fact]
    public async Task QuotaExceeded_sends_no_notification()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient { ThrowOnSync = new Exception("Quota exceeded for today") };
        var ntfy = new FakeNtfyNotifier();

        await Runner(db, frenoy, Settings(2024), ntfy).RunAsync();

        Assert.Empty(ntfy.Sent);
    }
}
