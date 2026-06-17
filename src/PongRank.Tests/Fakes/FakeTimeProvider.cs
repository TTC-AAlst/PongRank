namespace PongRank.Tests.Fakes;

internal class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;
    public FakeTimeProvider(DateTimeOffset now) => _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
}
