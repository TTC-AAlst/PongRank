using PongRank.Model;

namespace PongRank.WebApi.Utilities;

/// <summary>
/// One sync run's result for a single (Competition, Year). TournamentsAdded/MatchesAdded
/// are this-run deltas; the *Synced/*Total pairs are cumulative progress for the year.
/// </summary>
public record SyncSummary(
    Competition Competition,
    int Year,
    int MatchesAdded,
    int TournamentsAdded,
    int ClubsSynced,
    int ClubsTotal,
    int TournamentsSynced,
    int TournamentsTotal,
    string Outcome);

public interface INtfyNotifier
{
    /// <summary>
    /// Post a sync summary to ntfy (→ Slack #apps via the docker-01 bridge), every run —
    /// including no-change, quota-exceeded and error outcomes. No-op when no token is configured.
    /// </summary>
    Task NotifyAsync(SyncSummary summary);
}
