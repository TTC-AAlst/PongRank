using PongRank.DataEntities;
using PongRank.Model;
using PongRank.Tests.Fakes;
using Xunit;

namespace PongRank.Tests;

public class SyncStateEntityTests
{
    [Fact]
    public async Task SyncState_round_trips_with_status_as_string()
    {
        using var db = InMemoryDb.Create();
        db.SyncStates.Add(new SyncStateEntity
        {
            Competition = Competition.Vttl,
            Year = 2024,
            Status = SyncStatus.Closed,
            LastOutcome = "Completed",
            AttemptCount = 1,
        });
        await db.SaveChangesAsync();

        var loaded = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Closed, loaded.Status);
        Assert.Equal(2024, loaded.Year);
    }
}
