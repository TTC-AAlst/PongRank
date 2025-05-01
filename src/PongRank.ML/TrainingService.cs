using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using PongRank.DataEntities.Core;
using PongRank.Model;

namespace PongRank.ML
{
    public class TrainingService
    {
        private readonly ITtcDbContext _db;

        public TrainingService(ITtcDbContext db)
        {
            _db = db;
        }

        public async Task Train(Competition competition, int[] years)
        {
            var playerResults = await _db.PlayerResults
                .Where(x => x.Competition == competition && years.Contains(x.Year))
                .Where(x => x.NextRanking != null)
                .ToArrayAsync();

            var mlContext = new MLContext();
            var inputData = playerResults.Select(PlayerResultsInput.MapFromEntity).ToArray();
            IDataView trainingData = mlContext.Data.LoadFromEnumerable(inputData);

            var pipeline = mlContext
                .Transforms.Conversion.MapValueToKey(inputColumnName: nameof(PlayerResultsInput.NextRanking), outputColumnName: "Label")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("CategoryNameEncoded", nameof(PlayerResultsInput.CategoryName)))
                .Append(mlContext.Transforms.Concatenate("Features", GetInputColumnNames(competition)))
                .AppendCacheCheckpoint(mlContext)
                .Append(mlContext.MulticlassClassification.Trainers.LightGbm("Label", "Features"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var trainingModel = pipeline.Fit(trainingData);
            mlContext.Model.Save(trainingModel, trainingData.Schema, "SportaRankingModel.zip");
        }

        private static string[] GetInputColumnNames(Competition competition)
        {
            List<string> columns = new List<string>
            {
                nameof(PlayerResultsInput.Competition),
                nameof(PlayerResultsInput.Year),
                "CategoryNameEncoded",
                nameof(PlayerResultsInput.RankingValue),
                nameof(PlayerResultsInput.AWins),
                nameof(PlayerResultsInput.ALosses),
                nameof(PlayerResultsInput.B0Wins),
                nameof(PlayerResultsInput.B0Losses),
                nameof(PlayerResultsInput.B2Wins),
                nameof(PlayerResultsInput.B2Losses),
                nameof(PlayerResultsInput.B4Wins),
                nameof(PlayerResultsInput.B4Losses),
                nameof(PlayerResultsInput.B6Wins),
                nameof(PlayerResultsInput.B6Losses),
                nameof(PlayerResultsInput.C0Wins),
                nameof(PlayerResultsInput.C0Losses),
                nameof(PlayerResultsInput.C2Wins),
                nameof(PlayerResultsInput.C2Losses),
                nameof(PlayerResultsInput.C4Wins),
                nameof(PlayerResultsInput.C4Losses),
                nameof(PlayerResultsInput.C6Wins),
                nameof(PlayerResultsInput.C6Losses),
                nameof(PlayerResultsInput.D0Wins),
                nameof(PlayerResultsInput.D0Losses),
                nameof(PlayerResultsInput.D2Wins),
                nameof(PlayerResultsInput.D2Losses),
                nameof(PlayerResultsInput.D4Wins),
                nameof(PlayerResultsInput.D4Losses),
                nameof(PlayerResultsInput.D6Wins),
                nameof(PlayerResultsInput.D6Losses),
                nameof(PlayerResultsInput.E0Wins),
                nameof(PlayerResultsInput.E0Losses),
                nameof(PlayerResultsInput.E2Wins),
                nameof(PlayerResultsInput.E2Losses),
                nameof(PlayerResultsInput.E4Wins),
                nameof(PlayerResultsInput.E4Losses),
                nameof(PlayerResultsInput.E6Wins),
                nameof(PlayerResultsInput.E6Losses)
            };

            if (competition == Competition.Sporta)
            {
                columns.AddRange(new[]
                {
                    nameof(PlayerResultsInput.FWins),
                    nameof(PlayerResultsInput.FLosses),
                });
            }

            columns.AddRange(new[]
            {
                nameof(PlayerResultsInput.NGWins),
                nameof(PlayerResultsInput.NGLosses)
            });

            return columns.ToArray();
        }
    }
}
