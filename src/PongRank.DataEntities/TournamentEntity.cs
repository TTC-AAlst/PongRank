using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PongRank.Model;

namespace PongRank.DataEntities;

[Table("Tournaments")]
public class TournamentEntity
{
    public int Id { get; set; }
    [Column(TypeName = "character varying(10)")]
    public Competition Competition { get; set; }
    public int Year { get; set; }
    public DateTime Date { get; set; }
    [StringLength(10)]
    public string UniqueIndex { get; set; } = "";
    [StringLength(100)]
    public string Name { get; set; } = "";
    public bool SyncCompleted { get; set; }
    public int TotalMatches { get; set; }

    public override string ToString() => $"{Competition} {Year}: {Name} (SyncCompleted={SyncCompleted})";
}