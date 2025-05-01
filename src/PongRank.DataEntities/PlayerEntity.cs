using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using PongRank.Model;

namespace PongRank.DataEntities;

[Table("Players")]
[Index(nameof(Competition), nameof(Year))]
public class PlayerEntity
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "character varying(10)")]
    public Competition Competition { get; set; }
    public int Year { get; set; }
    [StringLength(50)]
    public string CategoryName { get; set; } = "";
    [StringLength(50)]
    public string ClubName { get; set; } = "";

    public int UniqueIndex { get; set; }

    [StringLength(100)]
    public string FirstName { get; set; } = "";
    [StringLength(100)]
    public string LastName { get; set; } = "";

    [StringLength(5)]
    public string Ranking { get; set; } = "";
    [StringLength(20)]
    public string Club { get; set; } = "";

    public override string ToString() => $"{Competition} {Year}: {FirstName} {LastName} @ {ClubName} ({Ranking})";
}
