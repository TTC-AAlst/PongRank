using PongRank.DataEntities;
using PongRank.Model;
using PongRank.Tests.Fakes;
using PongRank.WebApi.Controllers;
using Xunit;

namespace PongRank.Tests;

public class SyncStatusControllerTests
{
    [Fact]
    public async Task Returns_status_with_club_and_tournament_counts()
    {
        using var db = InMemoryDb.Create();
        db.SyncStates.Add(new SyncStateEntity { Competition = Competition.Vttl, Year = 2024, Status = SyncStatus.Closed, LastOutcome = "Completed", AttemptCount = 1 });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Oost-Vlaanderen", SyncCompleted = true });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Oost-Vlaanderen", SyncCompleted = false });
        db.Tournaments.Add(new TournamentEntity { Competition = Competition.Vttl, Year = 2024, SyncCompleted = true });
        await db.SaveChangesAsync();

        var result = await new SyncStatusController(db).Get(category: null);

        var row = Assert.Single(result);
        Assert.Equal("Vttl", row.Competition);
        Assert.Equal(2024, row.Year);
        Assert.Equal("Closed", row.Status);
        var cat = Assert.Single(row.Categories);
        Assert.Equal("Oost-Vlaanderen", cat.CategoryName);
        Assert.Equal(1, cat.ClubsSynced);
        Assert.Equal(1, cat.ClubsPending);
        Assert.Equal(1, row.Tournaments.Synced);
        Assert.Equal(0, row.Tournaments.Pending);
    }

    [Fact]
    public async Task Category_filter_limits_club_rows()
    {
        using var db = InMemoryDb.Create();
        db.SyncStates.Add(new SyncStateEntity { Competition = Competition.Vttl, Year = 2024, Status = SyncStatus.Pending });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Oost-Vlaanderen", SyncCompleted = true });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Antwerpen", SyncCompleted = true });
        await db.SaveChangesAsync();

        var result = await new SyncStatusController(db).Get(category: "Oost-Vlaanderen");

        var row = Assert.Single(result);
        var cat = Assert.Single(row.Categories);
        Assert.Equal("Oost-Vlaanderen", cat.CategoryName);
    }
}
