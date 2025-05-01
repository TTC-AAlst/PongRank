using PongRank.Model;

namespace PongRank.FrenoyApi;

public class FrenoySettings
{
    private int _year;

    public Competition Competition { get; }

    public int Year
    {
        get => _year;
        set
        {
            _year = value;
            if (_year < 1000)
            {
                _year += 2000 - 1;
            }
        }
    }

    public string[] CategoryNames { get; } = [];
    public int FrenoySeason => Year - 2000 + 1;

    public FrenoySettings(Competition competition, int year, string[] categoryNames)
    {
        Competition = competition;
        Year = year;
        CategoryNames = categoryNames;
    }

    public FrenoySettings()
    {
        
    }

    public override string ToString() => $"FrenoySeason={FrenoySeason}, Competition={Competition}, Year={Year}";
}
