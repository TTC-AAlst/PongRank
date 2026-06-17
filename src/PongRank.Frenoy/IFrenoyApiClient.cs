namespace PongRank.FrenoyApi;

/// <summary>The slice of <see cref="FrenoyApiClient"/> the sync jobs depend on (enables faking in tests).</summary>
public interface IFrenoyApiClient
{
    void Open(FrenoySettings settings);
    Task Sync();
    Task SyncMatches(string clubUniqueIndex);

    /// <summary>Frenoy quota seen on the most recent <see cref="GetQuotaAsync"/> call, or null if unknown.</summary>
    (int Current, int Allowed)? LastQuota { get; }

    /// <summary>Best-effort Test call to read the API quota. Returns null on any failure.</summary>
    Task<(int Current, int Allowed)?> GetQuotaAsync();
}
