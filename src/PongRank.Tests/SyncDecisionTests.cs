using PongRank.DataEntities;
using PongRank.WebApi.Utilities;
using Xunit;

namespace PongRank.Tests;

public class SyncDecisionTests
{
    [Fact]
    public void CurrentYear_stays_pending_even_when_nothing_left()
    {
        var o = SyncDecision.Evaluate(year: 2025, currentYear: 2025, nothingLeft: true, quotaExceeded: false);
        Assert.Equal(SyncStatus.Pending, o.Status);
        Assert.Equal("Completed", o.LastOutcome);
    }

    [Fact]
    public void PastYear_closes_as_completed_when_nothing_left()
    {
        var o = SyncDecision.Evaluate(year: 2024, currentYear: 2025, nothingLeft: true, quotaExceeded: false);
        Assert.Equal(SyncStatus.Closed, o.Status);
        Assert.Equal("Completed", o.LastOutcome);
    }

    [Fact]
    public void PastYear_closes_incomplete_when_stragglers_remain()
    {
        var o = SyncDecision.Evaluate(year: 2024, currentYear: 2025, nothingLeft: false, quotaExceeded: false);
        Assert.Equal(SyncStatus.Closed, o.Status);
        Assert.Equal("Closed-incomplete", o.LastOutcome);
    }

    [Fact]
    public void QuotaExceeded_keeps_pending_for_any_year()
    {
        var past = SyncDecision.Evaluate(year: 2024, currentYear: 2025, nothingLeft: false, quotaExceeded: true);
        Assert.Equal(SyncStatus.Pending, past.Status);
        Assert.Equal("QuotaExceeded", past.LastOutcome);
    }
}
