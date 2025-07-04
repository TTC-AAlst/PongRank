﻿using PongRank.Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PongRank.DataEntities;

[Table("Clubs")]
[Index(nameof(Competition), nameof(Year))]
public class ClubEntity
{
    [Key]
    public int Id { get; set; }
    [StringLength(50)]
    public string Name { get; set; } = "";

    [Column(TypeName = "character varying(10)")]
    public Competition Competition { get; set; }
    public int Year { get; set; }
    [StringLength(20)]
    public string UniqueIndex { get; set; } = "";

    public int Category { get; set; }
    [StringLength(50)]
    public string CategoryName { get; set; } = "";

    public bool SyncCompleted { get; set; }

    public override string ToString() => $"{Competition} {Year}: {Name} ({UniqueIndex}, {CategoryName}), SyncCompleted={SyncCompleted}";
}
