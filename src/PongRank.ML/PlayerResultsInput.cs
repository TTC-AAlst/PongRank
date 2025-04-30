using PongRank.DataEntities;
using PongRank.Model;
using System.Numerics;

namespace PongRank.ML;

public class PlayerResultsInput
{
    public float Competition { get; set; }
    public float Year { get; set; }
    public string CategoryName { get; set; } = "";

    public string Ranking { get; set; } = "";
    public float RankingValue { get; set; }

    public string NextRanking { get; set; } = "";
    public float NextRankingValue { get; set; }

    public float AWins { get; set; }
    public float ALosses { get; set; }

    public float B0Wins { get; set; }
    public float B0Losses { get; set; }

    public float B2Wins { get; set; }
    public float B2Losses { get; set; }

    public float B4Wins { get; set; }
    public float B4Losses { get; set; }

    public float B6Wins { get; set; }
    public float B6Losses { get; set; }

    public float C0Wins { get; set; }
    public float C0Losses { get; set; }

    public float C2Wins { get; set; }
    public float C2Losses { get; set; }

    public float C4Wins { get; set; }
    public float C4Losses { get; set; }

    public float C6Wins { get; set; }
    public float C6Losses { get; set; }

    public float D0Wins { get; set; }
    public float D0Losses { get; set; }

    public float D2Wins { get; set; }
    public float D2Losses { get; set; }

    public float D4Wins { get; set; }
    public float D4Losses { get; set; }

    public float D6Wins { get; set; }
    public float D6Losses { get; set; }

    public float E0Wins { get; set; }
    public float E0Losses { get; set; }

    public float E2Wins { get; set; }
    public float E2Losses { get; set; }

    public float E4Wins { get; set; }
    public float E4Losses { get; set; }

    public float E6Wins { get; set; }
    public float E6Losses { get; set; }

    public float FWins { get; set; }
    public float FLosses { get; set; }

    public float NGWins { get; set; }
    public float NGLosses { get; set; }

    internal static PlayerResultsInput MapFromEntity(PlayerResultsEntity entity)
    {
        return new PlayerResultsInput
        {
            Competition = (int)entity.Competition,
            Year = entity.Year,
            CategoryName = entity.CategoryName,
            Ranking = entity.Ranking,
            RankingValue = RankingValueConverter.Get(entity.Competition, entity.Ranking),
            NextRanking = entity.NextRanking ?? "",
            NextRankingValue = entity.NextRanking != null ? RankingValueConverter.Get(entity.Competition, entity.NextRanking) : 0,
            AWins = entity.AWins,
            ALosses = entity.ALosses,
            B0Wins = entity.B0Wins,
            B0Losses = entity.B0Losses,
            B2Wins = entity.B2Wins,
            B2Losses = entity.B2Losses,
            B4Wins = entity.B4Wins,
            B4Losses = entity.B4Losses,
            B6Wins = entity.B6Wins,
            B6Losses = entity.B6Losses,
            C0Wins = entity.C0Wins,
            C0Losses = entity.C0Losses,
            C2Wins = entity.C2Wins,
            C2Losses = entity.C2Losses,
            C4Wins = entity.C4Wins,
            C4Losses = entity.C4Losses,
            C6Wins = entity.C6Wins,
            C6Losses = entity.C6Losses,
            D0Wins = entity.D0Wins,
            D0Losses = entity.D0Losses,
            D2Wins = entity.D2Wins,
            D2Losses = entity.D2Losses,
            D4Wins = entity.D4Wins,
            D4Losses = entity.D4Losses,
            D6Wins = entity.D6Wins,
            D6Losses = entity.D6Losses,
            E0Wins = entity.E0Wins,
            E0Losses = entity.E0Losses,
            E2Wins = entity.E2Wins,
            E2Losses = entity.E2Losses,
            E4Wins = entity.E4Wins,
            E4Losses = entity.E4Losses,
            E6Wins = entity.E6Wins,
            E6Losses = entity.E6Losses,
            FWins = entity.FWins,
            FLosses = entity.FLosses,
            NGWins = entity.NGWins,
            NGLosses = entity.NGLosses
        };
    }

    public override string ToString() => $"{Competition} {Year}: {Ranking} -> {NextRanking}";
}