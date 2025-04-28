using PongRank.DataEntities;
using PongRank.Model;

namespace PongRank.ConsoleApp.Utilities;

public class ConsoleSettings
{
    public bool FrenoySync { get; set; }
    public Competition[] Competitions { get; set; } = [];
    public int[] Seasons { get; set; } = [];
    public string[] CategoryNames { get; set; } = [];

    public bool AggregateResults { get; set; }

    public override string ToString() => $"{string.Join(';', Competitions)} for seasons={string.Join(';', Seasons)}";
}