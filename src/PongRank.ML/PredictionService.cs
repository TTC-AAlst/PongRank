using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using PongRank.DataEntities.Core;
using PongRank.ML.Models;

namespace PongRank.ML;

public class PredictionService
{
    private readonly ITtcDbContext _db;
    private readonly MLSettings _settings;

    public PredictionService(ITtcDbContext db, MLSettings settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IEnumerable<PredictionResult>> Predict(PredictionRequest request)
    {
        var playerUniqueIndexes = await _db.Players
            .Where(x => x.Competition == request.Competition)
            .Where(x => x.Year == request.Year)
            .Where(x => x.Club == request.ClubUniqueIndex)
            .Select(x => x.UniqueIndex)
            .ToArrayAsync();

        var players = await _db.PlayerResults
            .Where(x => x.Competition == request.Competition)
            .Where(x => x.Year == request.Year)
            .Where(x => playerUniqueIndexes.Contains(x.UniqueIndex))
            .ToArrayAsync();

        var playerInputs = players.Select(PlayerResultsInput.MapFromEntity).ToArray();

        var fileName = Path.Combine(_settings.ModelLocation, $"{request.Competition}RankingModel.zip");
        await using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        var mlContext = new MLContext();
        ITransformer model = mlContext.Model.Load(stream, out _);


        IDataView inputData = mlContext.Data.LoadFromEnumerable(playerInputs);
        IDataView predictions = model.Transform(inputData);
        var results = mlContext.Data.CreateEnumerable<RankingPrediction>(predictions, reuseRowObject: false);

        //var predictionEngine = mlContext.Model.CreatePredictionEngine<PlayerResultsInput, RankingPrediction>(model);
        //var result = predictionEngine.Predict(playerInputs[0]);

        var result = players.Zip(results, (player, prediction) => new PredictionResult()
        {
            OldRanking = player.Ranking,
            NewRanking = prediction.Ranking,
            UniqueIndex = player.UniqueIndex,
            Name = player.Name
        })
            .Where(x => x.OldRanking != x.NewRanking)
            .ToArray();

        return result;
    }
}