namespace PongRank.ML.Models;

public class MLSettings
{
    /// <summary>
    /// Location of the model.zip
    /// </summary>
    public string ModelLocation { get; set; } = "";

    public int[] UseYears { get; set; } = [];
    public string[] UseCategoryNames { get; set; } = [];
}