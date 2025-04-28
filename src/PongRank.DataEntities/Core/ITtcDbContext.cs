using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace PongRank.DataEntities.Core;

public interface ITtcDbContext
{
    DbSet<PlayerEntity> Players { get; set; }
    DbSet<ClubEntity> Clubs { get; set; }
    DbSet<MatchEntity> Matches { get; set; }
    
    // TODO: Also need to keep track of Tournaments

    Task<int> SaveChangesAsync(CancellationToken token = default);

    DatabaseFacade Database { get; }
}
