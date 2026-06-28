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
    private readonly INtfyNotifier _notifier;

    public HistoricalSyncRunner(
        ITtcDbContext db,
        IFrenoyApiClient frenoy,
        SyncJobSettings settings,
        TimeProvider clock,
        ILogger<HistoricalSyncRunner> logger,
        INtfyNotifier notifier)
    {
        _db = db;
        _frenoy = frenoy;
        _settings = settings;
        _clock = clock;
        _logger = logger;
        _notifier = notifier;
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
            foreach (int year in _settings.SyncYears)
                // Quota is global — a quota-exhausted year stops the whole run.
                if (await SyncYear(competition, year, currentYear))
                    return;
    }

    // Syncs one (Competition, Year), records state, and posts a Slack summary for every
    // attempted year (success/quota/error alike). Returns true when quota was exhausted.
    private async Task<bool> SyncYear(Competition competition, int year, int currentYear)
    {
        var state = await GetOrCreateState(competition, year);
        if (state.Status == SyncStatus.Closed)
        {
            _logger.LogInformation("SyncRun {Competition} {Year} skipped (Closed)", competition, year);
            return false; // skipped years aren't notified — nothing was synced
        }

        int matchesBefore = await CountMatches(competition, year);
        int tournamentsBefore = (await CountTournaments(competition, year)).Synced;

        bool quotaExceeded = false;
        Exception? error = null;
        try
        {
            _frenoy.Open(new FrenoySettings(competition, year, _settings.SyncCategoryNames));
            await _frenoy.Sync();
        }
        catch (Exception ex) when (ex.Message.Contains("Quota exceeded"))
        {
            quotaExceeded = true;
            error = ex;
        }
        catch (Exception ex)
        {
            error = ex;
        }

        // Counted after the catch so quota/error runs still report their partial progress.
        int matchesAdded = await CountMatches(competition, year) - matchesBefore;
        var (clubsSynced, clubsTotal) = await CountClubs(competition, year);
        var (tournamentsSynced, tournamentsTotal) = await CountTournaments(competition, year);
        bool nothingLeft = clubsSynced == clubsTotal && tournamentsSynced == tournamentsTotal;

        var outcome = (error, quotaExceeded) switch
        {
            (not null, false) => new SyncOutcome(state.Status, "Error"), // non-quota error preserves Status
            _ => SyncDecision.Evaluate(year, currentYear, nothingLeft, quotaExceeded),
        };
        Apply(state, outcome);
        await _db.SaveChangesAsync();

        if (error is { } && !quotaExceeded)
            _logger.LogError(error, "SyncRun {Competition} {Year} outcome=Error {ErrorMessage}", competition, year, error.Message);
        else if (quotaExceeded)
            _logger.LogWarning("SyncRun {Competition} {Year} outcome=QuotaExceeded {ErrorMessage}", competition, year, error!.Message);
        else
            _logger.LogInformation(
                "SyncRun {Competition} {Year} outcome={Outcome} clubsSynced={ClubsSynced}/{ClubsTotal} matchesAdded={MatchesAdded} tournamentsSynced={TournamentsSynced}/{TournamentsTotal}",
                competition, year, outcome.LastOutcome, clubsSynced, clubsTotal,
                matchesAdded, tournamentsSynced, tournamentsTotal);

        await _notifier.NotifyAsync(new SyncSummary(competition, year, matchesAdded,
            tournamentsSynced - tournamentsBefore, clubsSynced, clubsTotal,
            tournamentsSynced, tournamentsTotal, outcome.LastOutcome));

        return quotaExceeded;
    }

    private void Apply(SyncStateEntity state, SyncOutcome outcome)
    {
        state.Status = outcome.Status;
        state.LastOutcome = outcome.LastOutcome;
        state.AttemptCount++;
        state.LastAttemptUtc = _clock.GetUtcNow().UtcDateTime;
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
