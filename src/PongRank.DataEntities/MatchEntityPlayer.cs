namespace PongRank.DataEntities;

public class MatchEntityPlayer
{
    public int PlayerUniqueIndex { get; set; }

    public int SetCount { get; set; }

    public override string ToString() => $"Player={PlayerUniqueIndex}, Sets={SetCount}";
}