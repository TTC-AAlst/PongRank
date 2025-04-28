using PongRank.DataEntities;
using PongRank.Model;

namespace PongRank.ConsoleApp.Utilities;

public class ConsoleSettings
{
    public Competition[] Competitions { get; set; } = [];
    public int[] Seasons { get; set; } = [];
    public ClubCategory[] Categories { get; set; } = [];

    public override string ToString() => $"{string.Join(';', Competitions)} for seasons={string.Join(';', Seasons)}";
}