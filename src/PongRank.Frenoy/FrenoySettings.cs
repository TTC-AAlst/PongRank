using PongRank.DataEntities;
using PongRank.Model;

namespace PongRank.FrenoyApi;

public class FrenoySettings
{
    public Competition Competition { get; }
    public int Year { get; }
    public string[] CategoryNames { get; } = [];
    public int FrenoySeason => Year - 2000 + 1;

    
    public FrenoySettings(Competition competition, int year, string[] categoryNames)
    {
        Competition = competition;
        Year = year;
        CategoryNames = categoryNames;
        if (year < 1000)
        {
            Year = year + 2000 - 1;
        }
    }

    public override string ToString() => $"FrenoySeason={FrenoySeason}, Competition={Competition}, Year={Year}";
}
