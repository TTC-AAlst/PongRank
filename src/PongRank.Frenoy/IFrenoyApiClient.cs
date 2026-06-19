namespace PongRank.FrenoyApi;

/// <summary>The slice of <see cref="FrenoyApiClient"/> the sync jobs depend on (enables faking in tests).</summary>
public interface IFrenoyApiClient
{
    void Open(FrenoySettings settings);
    Task Sync();
    Task SyncMatches(string clubUniqueIndex);
}
