using Microsoft.ML.Data;

namespace PongRank.ML.Models;

/// <summary>
/// Prediction from the model, mapped to <see cref="PredictionResult"/>
/// </summary>
public class RankingPrediction
{
    [ColumnName("PredictedLabel")]
    public string Ranking { get; set; } = "";

    public override string ToString() => Ranking;
}
