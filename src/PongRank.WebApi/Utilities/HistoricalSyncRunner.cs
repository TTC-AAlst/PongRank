using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PongRank.DataEntities;
using PongRank.DataEntities.Core;
using PongRank.FrenoyApi;
using PongRank.Model;

namespace PongRank.WebApi.Utilities;

/// <summary>
/// Historical sync loop: walks SyncCompetitions × SyncYears, skipping (Competition, Year)
/// pairs already Closed, closing past years after a clean pass, never closing the current
/// year, and stopping the whole run when Frenoy quota is exhausted.
/// </summary>
public class HistoricalSyncRunner
{
    private readonly ITtcDbContext _db;
    private readonly IFrenoyApiClient _frenoy;
    private readonly SyncJobSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<HistoricalSyncRunner> _logger;

    public HistoricalSyncRunner(
        ITtcDbContext db,
        IFrenoyApiClient frenoy,
        SyncJobSettings settings,
        TimeProvider clock,
        ILogger<HistoricalSyncRunner> logger)
    {
        _db = db;
        _frenoy = frenoy;
        _settings = settings;
        _clock = clock;
        _logger = logger;
    }

    // Season spans Aug–Jul: before September, "current" is the previous calendar year.
    private int CurrentYear
    {
        get
        {
            var now = _clock.GetUtcNow();
            return now.Month < 9 ? now.Year - 1 : now.Year;
        }
    }

    public async Task RunAsync()
    {
        int currentYear = CurrentYear;

        foreach (var competition in _settings.SyncCompetitions)
        {
            await LogQuotaAsync(competition, currentYear);

            foreach (int year in _settings.SyncYears)
            {
                var state = await GetOrCreateState(competition, year);
                if (state.Status == SyncStatus.Closed)
                {
                    _logger.LogInformation("SyncRun {Competition} {Year} skipped (Closed)", competition, year);
                    continue;
                }

                try
                {
                    _frenoy.Open(new FrenoySettings(competition, year, _settings.SyncCategoryNames));

                    int matchesBefore = await CountMatches(competition, year);
                    await _frenoy.Sync();
                    int matchesAfter = await CountMatches(competition, year);

                    var (clubsSynced, clubsTotal) = await CountClubs(competition, year);
                    var (tournamentsSynced, tournamentsTotal) = await CountTournaments(competition, year);
                    bool nothingLeft = clubsSynced == clubsTotal && tournamentsSynced == tournamentsTotal;

                    var outcome = SyncDecision.Evaluate(year, currentYear, nothingLeft, quotaExceeded: false);
                    Apply(state, outcome);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation(
                        "SyncRun {Competition} {Year} outcome={Outcome} clubsSynced={ClubsSynced}/{ClubsTotal} matchesAdded={MatchesAdded} tournamentsSynced={TournamentsSynced}/{TournamentsTotal}",
                        competition, year, outcome.LastOutcome, clubsSynced, clubsTotal,
                        matchesAfter - matchesBefore, tournamentsSynced, tournamentsTotal);
                }
                catch (Exception ex) when (ex.Message.Contains("Quota exceeded"))
                {
                    Apply(state, new SyncOutcome(SyncStatus.Pending, "QuotaExceeded"));
                    await _db.SaveChangesAsync();
                    _logger.LogWarning("SyncRun {Competition} {Year} outcome=QuotaExceeded", competition, year);
                    return; // quota is global — stop the whole run
                }
                catch (Exception ex)
                {
                    Apply(state, new SyncOutcome(state.Status, "Error"));
                    await _db.SaveChangesAsync();
                    _logger.LogError(ex, "SyncRun {Competition} {Year} outcome=Error {ErrorMessage}", competition, year, ex.Message);
                }
            }
        }
    }

    private void Apply(SyncStateEntity state, SyncOutcome outcome)
    {
        state.Status = outcome.Status;
        state.LastOutcome = outcome.LastOutcome;
        state.AttemptCount++;
        state.LastAttemptUtc = _clock.GetUtcNow().UtcDateTime;
    }

    private async Task LogQuotaAsync(Competition competition, int currentYear)
    {
        // Open a client so GetQuota targets the right competition endpoint.
        _frenoy.Open(new FrenoySettings(competition, currentYear, _settings.SyncCategoryNames));
        var quota = await _frenoy.GetQuotaAsync();
        if (quota is { } q)
            _logger.LogInformation("SyncQuota {Competition} current={QuotaCurrent} allowed={QuotaAllowed}", competition, q.Current, q.Allowed);
    }

    private async Task<SyncStateEntity> GetOrCreateState(Competition competition, int year)
    {
        var state = await _db.SyncStates
            .SingleOrDefaultAsync(x => x.Competition == competition && x.Year == year);
        if (state is null)
        {
            state = new SyncStateEntity { Competition = competition, Year = year, Status = SyncStatus.Pending };
            _db.SyncStates.Add(state);
        }
        return state;
    }

    private Task<int> CountMatches(Competition competition, int year) =>
        _db.Matches.CountAsync(x => x.Competition == competition && x.Year == year);

    private async Task<(int Synced, int Total)> CountClubs(Competition competition, int year)
    {
        var query = _db.Clubs.Where(x => x.Competition == competition && x.Year == year);
        if (_settings.SyncCategoryNames.Length > 0)
            query = query.Where(x => _settings.SyncCategoryNames.Contains(x.CategoryName));
        int total = await query.CountAsync();
        int synced = await query.CountAsync(x => x.SyncCompleted);
        return (synced, total);
    }

    private async Task<(int Synced, int Total)> CountTournaments(Competition competition, int year)
    {
        var query = _db.Tournaments.Where(x => x.Competition == competition && x.Year == year);
        int total = await query.CountAsync();
        int synced = await query.CountAsync(x => x.SyncCompleted);
        return (synced, total);
    }
}
