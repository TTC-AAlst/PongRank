using Microsoft.ML.Data;

namespace PongRank.ML;

public class RankingPrediction
{
    [ColumnName("PredictedLabel")]
    public string Ranking { get; set; } = "";

    public override string ToString() => Ranking;
}
