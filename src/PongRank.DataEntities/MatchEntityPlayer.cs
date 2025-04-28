using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PongRank.DataEntities;

public class MatchEntityPlayer
{
    [StringLength(10)]
    public string PlayerUniqueIndex { get; set; } = "";

    [StringLength(100)]
    public string FirstName { get; set; } = "";

    [StringLength(100)]
    public string LastName { get; set; } = "";

    [NotMapped]
    public string Name => $"{FirstName} {LastName}";

    public int SetCount { get; set; }
}