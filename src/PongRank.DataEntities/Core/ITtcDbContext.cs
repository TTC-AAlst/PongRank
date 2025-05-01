using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PongRank.DataEntities.Core;

public interface ITtcDbContext : IAsyncDisposable
{
    DbSet<PlayerEntity> Players { get; set; }
    DbSet<ClubEntity> Clubs { get; set; }
    DbSet<MatchEntity> Matches { get; set; }
    DbSet<PlayerResultsEntity> PlayerResults { get; set; }
    DbSet<TournamentEntity> Tournaments { get; set; }

    Task<int> SaveChangesAsync(CancellationToken token = default);

    DatabaseFacade Database { get; }
}
