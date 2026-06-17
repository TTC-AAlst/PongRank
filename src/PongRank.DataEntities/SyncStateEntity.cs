using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using PongRank.Model;

namespace PongRank.DataEntities;

/// <summary>Whether a (Competition, Year) may still be synced from Frenoy.</summary>
public enum SyncStatus
{
    /// <summary>Eligible to attempt.</summary>
    Pending,
    /// <summary>Terminal — never call the Frenoy API for this pair again.</summary>
    Closed,
}

[Table("SyncState")]
[Index(nameof(Competition), nameof(Year), IsUnique = true)]
public class SyncStateEntity
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "character varying(10)")]
    public Competition Competition { get; set; }

    public int Year { get; set; }

    public SyncStatus Status { get; set; }

    public DateTime? LastAttemptUtc { get; set; }

    public int AttemptCount { get; set; }

    [StringLength(30)]
    public string LastOutcome { get; set; } = "";

    public override string ToString() => $"{Competition} {Year}: {Status} ({LastOutcome})";
}
