using PongRank.DataEntities;

namespace PongRank.WebApi.Utilities;

/// <summary>Result of deciding what to record after a sync attempt of one (Competition, Year).</summary>
public record SyncOutcome(SyncStatus Status, string LastOutcome);

/// <summary>Pure rule for whether a (Competition, Year) is done and what to record.</summary>
public static class SyncDecision
{
    public static SyncOutcome Evaluate(int year, int currentYear, bool nothingLeft, bool quotaExceeded)
    {
        if (quotaExceeded)
            return new SyncOutcome(SyncStatus.Pending, "QuotaExceeded");

        // The current season keeps producing new matches, so it is never closed.
        if (year >= currentYear)
            return new SyncOutcome(SyncStatus.Pending, "Completed");

        return new SyncOutcome(SyncStatus.Closed, nothingLeft ? "Completed" : "Closed-incomplete");
    }
}
