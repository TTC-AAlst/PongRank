using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PongRank.Model;

namespace PongRank.DataEntities;

[Table("Matches")]
public class MatchEntity
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "character varying(10)")]
    public Competition Competition { get; set; }
    public int Year { get; set; }

    public DateTime Date { get; set; }
    [StringLength(5)]
    public string WeekName { get; set; } = "";

    [MaxLength(50)]
    public string MatchId { get; set; } = "";
    public int MatchUniqueId { get; set; }
    [StringLength(50)]
    public string TournamentUniqueIndex { get; set; } = "";

    public MatchEntityPlayer Home { get; set; } = new();
    public MatchEntityPlayer Away { get; set; } = new();

    [NotMapped]
    public string Score => $"{Home.SetCount}-{Away.SetCount}";

    public override string ToString() => $"{Competition} {Year}: {Home.PlayerUniqueIndex} vs {Away.PlayerUniqueIndex}: {Score}";
}