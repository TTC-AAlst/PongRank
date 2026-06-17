using PongRank.FrenoyApi;

namespace PongRank.Tests.Fakes;

/// <summary>Records Sync() calls and optionally throws to simulate quota/errors.</summary>
internal class FakeFrenoyApiClient : IFrenoyApiClient
{
    public List<(int Year, string Competition)> SyncedYears { get; } = new();
    public Func<FrenoySettings, Task>? OnSync { get; set; }
    public Exception? ThrowOnSync { get; set; }

    private FrenoySettings _settings = new();

    public void Open(FrenoySettings settings) => _settings = settings;

    public async Task Sync()
    {
        SyncedYears.Add((_settings.Year, _settings.Competition.ToString()));
        if (ThrowOnSync is not null) throw ThrowOnSync;
        if (OnSync is not null) await OnSync(_settings);
    }

    public Task SyncMatches(string clubUniqueIndex) => Task.CompletedTask;

    public (int Current, int Allowed)? LastQuota => (100, 200);
    public Task<(int Current, int Allowed)?> GetQuotaAsync() => Task.FromResult<(int, int)?>((100, 200));
}
