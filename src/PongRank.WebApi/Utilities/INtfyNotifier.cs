using PongRank.Model;

namespace PongRank.WebApi.Utilities;

public interface INtfyNotifier
{
    /// <summary>
    /// Post a sync summary to ntfy (→ Slack #apps via the docker-01 bridge).
    /// No-op when no token is configured.
    /// </summary>
    Task SyncCompletedAsync(Competition competition, int year, int newMatches,
        int clubsSynced, int clubsTotal, int tournamentsSynced, int tournamentsTotal);
}
