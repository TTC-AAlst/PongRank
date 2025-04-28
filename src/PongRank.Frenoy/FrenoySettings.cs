using PongRank.DataEntities;
using PongRank.Model;

namespace PongRank.FrenoyApi;

public class FrenoySettings
{
    public Competition Competition { get; }
    public int Year { get; }
    public ClubCategory[] Categories { get; }
    public int FrenoySeason => Year - 2000 + 1;

    
    public FrenoySettings(Competition competition, int year, ClubCategory[] categories)
    {
        Competition = competition;
        Year = year;
        Categories = categories;
        if (year < 1000)
        {
            Year = year + 2000 - 1;
        }
    }

    public override string ToString() => $"FrenoySeason={FrenoySeason}, Competition={Competition}, Year={Year}";
}
