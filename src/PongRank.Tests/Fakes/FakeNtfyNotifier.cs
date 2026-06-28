using PongRank.WebApi.Utilities;

namespace PongRank.Tests.Fakes;

/// <summary>Records NotifyAsync calls so tests can assert what (if anything) was notified.</summary>
internal class FakeNtfyNotifier : INtfyNotifier
{
    public List<SyncSummary> Sent { get; } = new();

    public Task NotifyAsync(SyncSummary summary)
    {
        Sent.Add(summary);
        return Task.CompletedTask;
    }
}
