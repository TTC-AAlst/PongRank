using PongRank.Model;

namespace PongRank.WebApi.Utilities;

public class SyncJobSettings
{
    public int[] SyncYears { get; set; } = [];
    public string[] SyncCategoryNames { get; set; } = [];
    public Competition[] SynCompetitions { get; set; } = [];
}