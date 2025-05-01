namespace PongRank.ML.Models;

/// <summary>
/// The ML <see cref="RankingPrediction"/> is mapped
/// to this model and returned to the user
/// </summary>
public class PredictionResult
{
    public int UniqueIndex { get; set; }
    public string Name { get; set; } = "";
    public string OldRanking { get; set; } = "";
    public string NewRanking { get; set; } = "";

    public override string ToString() => $"{Name}: {OldRanking} -> {NewRanking}";
}