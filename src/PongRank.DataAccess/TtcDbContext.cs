using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PongRank.DataEntities;
using PongRank.DataEntities.Core;

namespace PongRank.DataAccess;

internal class TtcDbContext : DbContext, ITtcDbContext
{
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<ClubEntity> Clubs { get; set; }
    public DbSet<MatchEntity> Matches { get; set; }
    public DbSet<PlayerResultsEntity> PlayerResults { get; set; }

    public TtcDbContext(DbContextOptions<TtcDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Get the same time as the Frenoy Api
    /// </summary>
    public static DateTime GetCurrentBelgianDateTime()
    {
        DateTime belgianTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Romance Standard Time");
        return belgianTime;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchEntity>(entity =>
        {
            entity.OwnsOne(e => e.Home);
            entity.OwnsOne(e => e.Away);
        });
    }
}

/// <summary>
/// For EF Migrations
/// </summary>
internal class TtcDbContextFactory : IDesignTimeDbContextFactory<TtcDbContext>
{
    public TtcDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TtcDbContext>();
        GlobalBackendConfiguration.ConfigureDbContextBuilder(builder);
        return new TtcDbContext(builder.Options);
    }
}
