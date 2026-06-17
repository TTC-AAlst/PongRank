using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PongRank.DataEntities.Core;

namespace PongRank.WebApi.Controllers;

/// <summary>Current sync state per (Competition, Year): the SQL progress query as JSON.</summary>
[Route("api/[controller]")]
public class SyncStatusController
{
    private readonly ITtcDbContext _db;

    public SyncStatusController(ITtcDbContext db) => _db = db;

    /// <summary>Sync state with club/tournament SyncCompleted counts. Optional category filter (clubs only).</summary>
    [HttpGet]
    public async Task<List<SyncStatusRow>> Get([FromQuery] string? category)
    {
        var states = await _db.SyncStates
            .OrderBy(x => x.Competition).ThenByDescending(x => x.Year)
            .ToListAsync();

        var clubs = await _db.Clubs
            .Where(c => category == null || c.CategoryName == category)
            .GroupBy(c => new { c.Competition, c.Year, c.CategoryName })
            .Select(g => new
            {
                g.Key.Competition,
                g.Key.Year,
                g.Key.CategoryName,
                Synced = g.Count(x => x.SyncCompleted),
                Pending = g.Count(x => !x.SyncCompleted),
            })
            .ToListAsync();

        var tournaments = await _db.Tournaments
            .GroupBy(t => new { t.Competition, t.Year })
            .Select(g => new
            {
                g.Key.Competition,
                g.Key.Year,
                Synced = g.Count(x => x.SyncCompleted),
                Pending = g.Count(x => !x.SyncCompleted),
            })
            .ToListAsync();

        return states.Select(s =>
        {
            var tour = tournaments.FirstOrDefault(t => t.Competition == s.Competition && t.Year == s.Year);
            return new SyncStatusRow
            {
                Competition = s.Competition.ToString(),
                Year = s.Year,
                Status = s.Status.ToString(),
                LastAttemptUtc = s.LastAttemptUtc,
                Attempts = s.AttemptCount,
                LastOutcome = s.LastOutcome,
                Categories = clubs
                    .Where(c => c.Competition == s.Competition && c.Year == s.Year)
                    .Select(c => new CategoryCount { CategoryName = c.CategoryName, ClubsSynced = c.Synced, ClubsPending = c.Pending })
                    .ToList(),
                Tournaments = new SyncedPending { Synced = tour?.Synced ?? 0, Pending = tour?.Pending ?? 0 },
            };
        }).ToList();
    }
}

#pragma warning disable CS1591
public class SyncStatusRow
{
    public string Competition { get; set; } = "";
    public int Year { get; set; }
    public string Status { get; set; } = "";
    public DateTime? LastAttemptUtc { get; set; }
    public int Attempts { get; set; }
    public string LastOutcome { get; set; } = "";
    public List<CategoryCount> Categories { get; set; } = new();
    public SyncedPending Tournaments { get; set; } = new();
}

public class CategoryCount
{
    public string CategoryName { get; set; } = "";
    public int ClubsSynced { get; set; }
    public int ClubsPending { get; set; }
}

public class SyncedPending
{
    public int Synced { get; set; }
    public int Pending { get; set; }
}
#pragma warning restore CS1591
