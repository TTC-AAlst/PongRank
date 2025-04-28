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
    public int UniqueIndex { get; set; }

    [StringLength(100)]
    public string FirstName { get; set; } = "";
    [StringLength(100)]
    public string LastName { get; set; } = "";

    [StringLength(5)]
    public string Ranking { get; set; } = "";
    [StringLength(5)]
    public string? NextRanking { get; set; }
    public int Club { get; set; }

    public override string ToString() => $"Id={Id}, Name={FirstName} {LastName}, {Competition}={Ranking} -> {NextRanking}";
}
