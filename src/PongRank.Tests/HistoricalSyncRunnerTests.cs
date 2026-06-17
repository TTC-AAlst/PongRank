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

    private static HistoricalSyncRunner Runner(TtcDbContext db, FakeFrenoyApiClient frenoy, SyncJobSettings settings) =>
        new(db, frenoy, settings, Clock, NullLogger<HistoricalSyncRunner>.Instance);

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
}
