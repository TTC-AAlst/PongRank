using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PongRank.DataEntities;
using PongRank.DataEntities.Core;
using PongRank.Model;

namespace PongRank.DataAccess;

/// <summary>
/// Step 2: Aggregate the data from the FrenoyApi sync to <see cref="PlayerResultsEntity"/> records
/// </summary>
public class AggregateService
{
    private const int BatchSize = 100;
    private readonly ITtcDbContext _db;
    private readonly ILogger<AggregateService> _logger;

    public AggregateService(ITtcDbContext db, ILogger<AggregateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CalculateAndSave(Competition competition)
    {
        var years = await _db.Clubs
            .Where(x => x.Competition == competition)
            .GroupBy(x => x.Year)
            .Where(x => x.All(c => c.SyncCompleted))
            .Select(x => x.Key)
            .ToArrayAsync();

        _logger.LogInformation("Aggregating for {competition} and {years}", competition, years);
        foreach (var year in years)
        {
            await CalculateAndSave(competition, year);
        }
    }

    public async Task CalculateAndSave(Competition competition, int year, string? clubUniqueIndex = null)
    {
        // ATTN: We are allowing this right now because we're just going to train on Oost-Vlaanderen?
        // ATTN: I'm not sure if this would even be an issue, we can train on what is already available?
        //bool dataIncomplete = await _db.Clubs
        //    .Where(x => x.Competition == competition)
        //    .Where(x => x.Year == year)
        //    .AnyAsync(x => !x.SyncCompleted);

        //if (dataIncomplete)
        //    throw new Exception("Cannot aggregate results for a competition+year that is not yet fully synced");

        _logger.LogInformation("Start aggregation for {competition} {year}", competition, year);
        int deletedCount = await _db.PlayerResults
            .Where(x => x.Competition == competition && x.Year == year)
            .ExecuteDeleteAsync();

        _logger.LogInformation("Deleted {deletedCount} existing PlayerResult records", deletedCount);

        var players = await _db.Players.Where(x => x.Competition == competition && x.Year == year).ToArrayAsync();
        var nextYearPlayers = await _db.Players.Where(x => x.Competition == competition && x.Year == year + 1).ToArrayAsync();
        var matches = await _db.Matches.Where(x => x.Competition == competition && x.Year == year).ToArrayAsync();

        int counter = 0;

        var playerLookup = players.ToDictionary(x => x.UniqueIndex, x => x);
        _logger.LogInformation("Aggregating for #{PlayerCount} players", players.Length);

        if (clubUniqueIndex != null)
            players = players.Where(x => x.Club == clubUniqueIndex).ToArray();

        foreach (var player in players)
        {
            _logger.LogInformation("Aggregating for {playerName} ({playerRanking})", player.FirstName + " " + player.LastName, player.Ranking);
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
                if (playerLookup.TryGetValue(game.Away.PlayerUniqueIndex, out PlayerEntity? opponent))
                {
                    bool won = game.Home.SetCount > game.Away.SetCount;
                    playerResults.AddGame(opponent.Ranking, won);
                }
                else
                {
                    _logger.LogWarning("Could not find opponent player with uniqueIndex {uniqueIndex} ({competition} {year})", game.Away.PlayerUniqueIndex, competition, year);
                }
            }

            games = [.. matches.Where(x => x.Away.PlayerUniqueIndex == player.UniqueIndex)];
            playerResults.TotalGames += games.Length;
            foreach (var game in games)
            {
                if (playerLookup.TryGetValue(game.Home.PlayerUniqueIndex, out PlayerEntity? opponent))
                {
                    bool won = game.Away.SetCount > game.Home.SetCount;
                    playerResults.AddGame(opponent.Ranking, won);
                }
                else
                {
                    _logger.LogWarning("Could not find opponent player with uniqueIndex {uniqueIndex} ({competition} {year})", game.Home.PlayerUniqueIndex, competition, year);
                }
            }

            await _db.PlayerResults.AddAsync(playerResults);

            counter++;
            if (counter % BatchSize == 0)
                await _db.SaveChangesAsync();
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Aggregation done for {competition} {year}", competition, year);
    }

    public async Task SetRankingsPreviousYear()
    {
        int currentYear = DateTime.Now.Year;
        await SetRankingsPreviousYear(Competition.Vttl, currentYear);
        await SetRankingsPreviousYear(Competition.Sporta, currentYear);
    }

    private async Task SetRankingsPreviousYear(Competition competition, int currentYear)
    {
        _logger.LogInformation("SetRankingsPreviousYear for {competition} {lastYear}", competition, currentYear - 1);
        var lastYearPlayers = await _db.PlayerResults.Where(x => x.Competition == competition && x.Year == currentYear - 1).ToArrayAsync();
        var currentYearPlayers = await _db.Players.Where(x => x.Competition == competition && x.Year == currentYear).ToArrayAsync();

        int counter = 0;

        _logger.LogInformation("SetRankingsPreviousYear: updating for {playerCount}", lastYearPlayers.Length);
        foreach (var lastYearPlayer in lastYearPlayers)
        {
            var currentYearPlayer = currentYearPlayers.FirstOrDefault(x => x.UniqueIndex == lastYearPlayer.UniqueIndex);
            if (currentYearPlayer == null)
                continue;

            lastYearPlayer.NextRanking = currentYearPlayer.Ranking;

            counter++;
            if (counter % BatchSize == 0)
                await _db.SaveChangesAsync();
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("SetRankingsPreviousYear DONE");
    }
}
