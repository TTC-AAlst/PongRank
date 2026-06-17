using Microsoft.EntityFrameworkCore;
using PongRank.DataAccess;

namespace PongRank.Tests.Fakes;

internal static class InMemoryDb
{
    /// <summary>Fresh isolated in-memory TtcDbContext (unique db name per call).</summary>
    public static TtcDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TtcDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TtcDbContext(options);
    }
}
