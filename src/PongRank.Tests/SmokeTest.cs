using PongRank.Tests.Fakes;
using Xunit;

namespace PongRank.Tests;

public class SmokeTest
{
    [Fact]
    public void InMemoryDb_can_be_created()
    {
        using var db = InMemoryDb.Create();
        Assert.NotNull(db.Clubs);
    }
}
