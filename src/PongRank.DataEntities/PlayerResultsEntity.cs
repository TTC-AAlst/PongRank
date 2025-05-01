using PongRank.Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace PongRank.DataEntities;

[Table("PlayerResults")]
public class PlayerResultsEntity
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "character varying(10)")]
    public Competition Competition { get; set; }
    public int Year { get; set; }
    [StringLength(50)]
    public string CategoryName { get; set; } = "";
    public int UniqueIndex { get; set; }

    [StringLength(100)]
    public string FirstName { get; set; } = "";
    [StringLength(100)]
    public string LastName { get; set; } = "";
    [NotMapped]
    public string Name => $"{FirstName} {LastName}";

    [StringLength(5)]
    public string Ranking { get; set; } = "";

    [StringLength(5)]
    public string? NextRanking { get; set; }

    public int TotalGames { get; set; }

    public int AWins { get; set; }
    public int ALosses { get; set; }

    public int B0Wins { get; set; }
    public int B0Losses { get; set; }

    public int B2Wins { get; set; }
    public int B2Losses { get; set; }

    public int B4Wins { get; set; }
    public int B4Losses { get; set; }

    public int B6Wins { get; set; }
    public int B6Losses { get; set; }

    public int C0Wins { get; set; }
    public int C0Losses { get; set; }

    public int C2Wins { get; set; }
    public int C2Losses { get; set; }

    public int C4Wins { get; set; }
    public int C4Losses { get; set; }

    public int C6Wins { get; set; }
    public int C6Losses { get; set; }

    public int D0Wins { get; set; }
    public int D0Losses { get; set; }

    public int D2Wins { get; set; }
    public int D2Losses { get; set; }

    public int D4Wins { get; set; }
    public int D4Losses { get; set; }

    public int D6Wins { get; set; }
    public int D6Losses { get; set; }

    public int E0Wins { get; set; }
    public int E0Losses { get; set; }

    public int E2Wins { get; set; }
    public int E2Losses { get; set; }

    public int E4Wins { get; set; }
    public int E4Losses { get; set; }

    public int E6Wins { get; set; }
    public int E6Losses { get; set; }

    public int FWins { get; set; }
    public int FLosses { get; set; }

    public int NGWins { get; set; }
    public int NGLosses { get; set; }

    public void AddGame(string ranking, bool won)
    {
        switch (ranking)
        {
            case "NG":
                if (won) NGWins++; else NGLosses++;
                break;
            case "F":
                if (won) FWins++; else FLosses++;
                break;
            case "E6":
                if (won) E6Wins++; else E6Losses++;
                break;
            case "E4":
                if (won) E4Wins++; else E4Losses++;
                break;
            case "E2":
                if (won) E2Wins++; else E2Losses++;
                break;
            case "E0":
                if (won) E0Wins++; else E0Losses++;
                break;
            case "D6":
                if (won) D6Wins++; else D6Losses++;
                break;
            case "D4":
                if (won) D4Wins++; else D4Losses++;
                break;
            case "D2":
                if (won) D2Wins++; else D2Losses++;
                break;
            case "D0":
                if (won) D0Wins++; else D0Losses++;
                break;
            case "C6":
                if (won) C6Wins++; else C6Losses++;
                break;
            case "C4":
                if (won) C4Wins++; else C4Losses++;
                break;
            case "C2":
                if (won) C2Wins++; else C2Losses++;
                break;
            case "C0":
                if (won) C0Wins++; else C0Losses++;
                break;
            case "B6":
                if (won) B6Wins++; else B6Losses++;
                break;
            case "B4":
                if (won) B4Wins++; else B4Losses++;
                break;
            case "B2":
                if (won) B2Wins++; else B2Losses++;
                break;
            case "B0":
                if (won) B0Wins++; else B0Losses++;
                break;
            case "A":
                if (won) AWins++; else ALosses++;
                break;
            default:
                if (ranking.StartsWith("A"))
                {
                    if (won) AWins++; else ALosses++;
                }
                else
                {
                    Debug.Assert(false, $"Unexpected ranking '{ranking}'");
                }
                break;
        }
    }

    public override string ToString() => $"{Competition} {Year}: {FirstName} {LastName}: {Ranking} -> {NextRanking} (#{TotalGames})";
}