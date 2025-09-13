using PongRank.Model;

namespace PongRank.FrenoyApi;

public class FrenoySettings
{
    private int _year;

    public Competition Competition { get; set; }

    /// <summary>
    /// The actual year (not Frenoy season)
    /// </summary>
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

    /// <summary>
    /// Sporta: Oost-Vlaanderen, Antwerpen, Vl.-Brabant &amp; BHG  
    /// Vttl: Oost-Vlaanderen, West-Vlaanderen, Antwerpen, Limburg, ...
    /// </summary>
    public string[] CategoryNames { get; set; } = [];

    /// <summary>
    /// Calculated property (do not set in Swagger)
    /// </summary>
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
