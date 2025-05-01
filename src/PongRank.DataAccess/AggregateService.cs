using Microsoft.EntityFrameworkCore;
using PongRank.DataEntities;
using PongRank.DataEntities.Core;
using PongRank.Model;
using PongRank.Model.Core;

namespace PongRank.DataAccess;

/// <summary>
/// Aggregate the <see cref="PlayerResultsEntity"/>
/// </summary>
public class AggregateService
{
    private const int BatchSize = 100;
    private readonly ITtcDbContext _db;
    private readonly TtcLogger _logger;

    public AggregateService(ITtcDbContext db, TtcLogger logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CalculateAndSave(Competition competition, int year)
    {
        await _db.PlayerResults
            .Where(x => x.Competition == competition && x.Year == year)
            .ExecuteDeleteAsync();

        var players = await _db.Players.Where(x => x.Competition == competition && x.Year == year).ToArrayAsync();
        var nextYearPlayers = await _db.Players.Where(x => x.Competition == competition && x.Year == year + 1).ToArrayAsync();
        var matches = await _db.Matches.Where(x => x.Competition == competition && x.Year == year).ToArrayAsync();

        int counter = 0;

        var playerLookup = players.ToDictionary(x => x.UniqueIndex, x => x);
        _logger.Information($"Aggregating for #{players.Length} players");
        foreach (var player in players)
        {
            var playerResults = new PlayerResultsEntity()
            {
                Competition = competition,
                Year = year,
                CategoryName = player.CategoryName,
                FirstName = player.FirstName,
                LastName = player.LastName,
                Ranking = player.Ranking,
                UniqueIndex = player.UniqueIndex,
            };

            var nextYear = nextYearPlayers.FirstOrDefault(x => x.UniqueIndex == player.UniqueIndex);
            if (nextYear != null)
            {
                playerResults.NextRanking = nextYear.Ranking;
            }

            MatchEntity[] games = [.. matches.Where(x => x.Home.PlayerUniqueIndex == player.UniqueIndex)];
            playerResults.TotalGames += games.Length;
            foreach (var game in games)
            {
                var opponent = playerLookup[game.Away.PlayerUniqueIndex];
                bool won = game.Home.SetCount > game.Away.SetCount;
                playerResults.AddGame(opponent.Ranking, won);
            }

            games = [.. matches.Where(x => x.Away.PlayerUniqueIndex == player.UniqueIndex)];
            playerResults.TotalGames += games.Length;
            foreach (var game in games)
            {
                var opponent = playerLookup[game.Home.PlayerUniqueIndex];
                bool won = game.Away.SetCount > game.Home.SetCount;
                playerResults.AddGame(opponent.Ranking, won);
            }

            if (playerResults.TotalGames > 8)
            {
                await _db.PlayerResults.AddAsync(playerResults);

                counter++;
                if (counter % BatchSize == 0)
                    await _db.SaveChangesAsync();
            }
        }

        await _db.SaveChangesAsync();
    }
}