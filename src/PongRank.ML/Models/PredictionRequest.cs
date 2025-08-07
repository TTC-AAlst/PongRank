using PongRank.Model;

namespace PongRank.ML.Models;

/// <summary>
/// Predict next ranking for entire club
/// </summary>
public class PredictionRequest
{
    public Competition Competition { get; set; }
    /// <summary>
    /// The actual year (not the Frenoy season)
    /// </summary>
    public int Year { get; set; }
    /// <summary>
    /// TTC Aalst Sporta: 4055  
    /// TTC Aalst Vttl: OVL134
    /// </summary>
    public string ClubUniqueIndex { get; set; } = "";

    public override string ToString() => $"{Competition} {Year}, Club={ClubUniqueIndex}";
}