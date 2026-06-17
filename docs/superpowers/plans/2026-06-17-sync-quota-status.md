# Sync Quota Control + Status — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop spending Frenoy API quota on years that can no longer yield new data, expose sync status via HTTP, and visualize sync runs in Grafana.

**Architecture:** A new `SyncState` table tracks per-`(Competition, Year)` status (`Pending`/`Closed`). The historical sync loop moves out of `FrenoySyncJob` (Timer plumbing) into a DI-injected, unit-testable `HistoricalSyncRunner` that skips `Closed` years, closes past years after a clean pass, never closes the current year, and stops the whole run when quota is exhausted. A status controller projects the DB truth as JSON; structured `SyncRun`/`SyncQuota` log lines feed a Loki Grafana dashboard.

**Tech Stack:** .NET 10, EF Core 10 (Npgsql), xUnit + EF InMemory, ASP.NET Core (Itenium.Forge), Serilog→Loki, Grafana/Loki.

**Spec:** `docs/superpowers/specs/2026-06-17-sync-quota-status-design.md`

**Conventions in this repo:**
- `TreatWarningsAsErrors=true`, `Nullable=enable`, `ImplicitUsings=enable`, `GenerateDocumentationFile=true` (`Directory.Build.props`). Public types need XML doc or they break the build (CS1591 is in `NoWarn`, so missing docs are OK — but keep warnings at zero).
- Central package management (`Directory.Packages.props`) — versions go there, `PackageReference` in csproj carries no `Version`.
- Build: `dotnet build PongRank.slnx`. Test (after Task 1): `dotnet test src/PongRank.Tests/PongRank.Tests.csproj`.
- Enums stored as text use `[Column(TypeName = "character varying(N)")]` (see `ClubEntity.Competition`); for the new `Status` enum we add an explicit `.HasConversion<string>()` in `OnModelCreating` to be safe.

---

## File Structure

**Create (pong-rank):**
- `src/PongRank.DataEntities/SyncStateEntity.cs` — entity + `SyncStatus` enum
- `src/PongRank.WebApi/Utilities/SyncDecision.cs` — pure close/outcome decision
- `src/PongRank.WebApi/Utilities/HistoricalSyncRunner.cs` — the testable loop
- `src/PongRank.WebApi/Controllers/SyncStatusController.cs` — `GET /api/SyncStatus` + DTOs
- `src/PongRank.Frenoy/IFrenoyApiClient.cs` — abstraction for the runner
- `src/PongRank.Tests/PongRank.Tests.csproj` — test project
- `src/PongRank.Tests/SyncDecisionTests.cs`
- `src/PongRank.Tests/HistoricalSyncRunnerTests.cs`
- `src/PongRank.Tests/SyncStatusControllerTests.cs`
- `src/PongRank.Tests/Fakes/FakeFrenoyApiClient.cs`
- `src/PongRank.Tests/Fakes/InMemoryDb.cs`
- `src/PongRank.DataAccess/Migrations/<generated>_SyncState.cs` (via `dotnet ef`)
- `homelab/obs-01/grafana/provisioning/dashboards/06-ttc-pong-sync.json` (**Home** repo)

**Modify (pong-rank):**
- `src/PongRank.DataEntities/Core/ITtcDbContext.cs` — add `DbSet<SyncStateEntity>`
- `src/PongRank.DataAccess/TtcDbContext.cs` — add `DbSet`, `OnModelCreating` conversion, `InternalsVisibleTo`
- `src/PongRank.Frenoy/FrenoyApiClient.cs` — implement `IFrenoyApiClient`, add `LastQuota` + `GetQuotaAsync()`
- `src/PongRank.WebApi/Utilities/FrenoySyncJob.cs` — delegate to `HistoricalSyncRunner`
- `src/PongRank.WebApi/Program.cs` — register `TimeProvider`, `IFrenoyApiClient`, `HistoricalSyncRunner`
- `Directory.Packages.props` — test + EF InMemory package versions
- `PongRank.slnx` — add test project

---

## Task 1: Test project + scaffolding

**Files:**
- Create: `src/PongRank.Tests/PongRank.Tests.csproj`
- Modify: `Directory.Packages.props`, `PongRank.slnx`, `src/PongRank.DataAccess/TtcDbContext.cs`
- Create: `src/PongRank.Tests/Fakes/InMemoryDb.cs`

> ⚠️ **New dependencies** — requires user approval before running: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.EntityFrameworkCore.InMemory`.

- [ ] **Step 1: Add package versions**

In `Directory.Packages.props`, inside the `<ItemGroup>`, add:

```xml
    <!-- Testing -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.5" />
```

- [ ] **Step 2: Create the test project**

`src/PongRank.Tests/PongRank.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../PongRank.WebApi/PongRank.WebApi.csproj" />
    <ProjectReference Include="../PongRank.DataAccess/PongRank.DataAccess.csproj" />
    <ProjectReference Include="../PongRank.DataEntities/PongRank.DataEntities.csproj" />
    <ProjectReference Include="../PongRank.Model/PongRank.Model.csproj" />
    <ProjectReference Include="../PongRank.Frenoy/PongRank.FrenoyApi.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Expose internals to tests**

At the top of `src/PongRank.DataAccess/TtcDbContext.cs`, after the `using` lines and before `namespace`, add:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PongRank.Tests")]
```

- [ ] **Step 4: Add test project to the solution**

Run: `dotnet sln PongRank.slnx add src/PongRank.Tests/PongRank.Tests.csproj`
Expected: "Project ... added to the solution." (If `dotnet sln` rejects `.slnx`, hand-edit `PongRank.slnx` to add a `<Project Path="src/PongRank.Tests/PongRank.Tests.csproj" />` entry mirroring the existing ones.)

- [ ] **Step 5: Create an InMemory DbContext helper**

`src/PongRank.Tests/Fakes/InMemoryDb.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using PongRank.DataAccess;

namespace PongRank.Tests.Fakes;

internal static class InMemoryDb
{
    /// <summary>Fresh isolated in-memory TtcDbContext (unique db name per call).</summary>
    public static TtcDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TtcDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TtcDbContext(options);
    }
}
```

- [ ] **Step 6: Add a smoke test**

`src/PongRank.Tests/SmokeTest.cs`:

```csharp
using PongRank.Tests.Fakes;
using Xunit;

namespace PongRank.Tests;

public class SmokeTest
{
    [Fact]
    public void InMemoryDb_can_be_created()
    {
        using var db = InMemoryDb.Create();
        Assert.NotNull(db.Clubs);
    }
}
```

- [ ] **Step 7: Run the test**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj`
Expected: 1 passing test.

- [ ] **Step 8: Commit**

```bash
git add Directory.Packages.props PongRank.slnx src/PongRank.Tests src/PongRank.DataAccess/TtcDbContext.cs
git commit -m "test: add xUnit test project with EF InMemory harness"
```

---

## Task 2: `SyncStateEntity` + migration

**Files:**
- Create: `src/PongRank.DataEntities/SyncStateEntity.cs`
- Modify: `src/PongRank.DataEntities/Core/ITtcDbContext.cs`, `src/PongRank.DataAccess/TtcDbContext.cs`

- [ ] **Step 1: Write the failing test**

`src/PongRank.Tests/SyncStateEntityTests.cs`:

```csharp
using PongRank.DataEntities;
using PongRank.Model;
using PongRank.Tests.Fakes;
using Xunit;

namespace PongRank.Tests;

public class SyncStateEntityTests
{
    [Fact]
    public async Task SyncState_round_trips_with_status_as_string()
    {
        using var db = InMemoryDb.Create();
        db.SyncStates.Add(new SyncStateEntity
        {
            Competition = Competition.Vttl,
            Year = 2024,
            Status = SyncStatus.Closed,
            LastOutcome = "Completed",
            AttemptCount = 1,
        });
        await db.SaveChangesAsync();

        var loaded = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Closed, loaded.Status);
        Assert.Equal(2024, loaded.Year);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj`
Expected: compile error — `SyncStateEntity`/`SyncStatus`/`db.SyncStates` not found.

- [ ] **Step 3: Create the entity**

`src/PongRank.DataEntities/SyncStateEntity.cs`:

```csharp
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
```

- [ ] **Step 4: Add to the context interface**

In `src/PongRank.DataEntities/Core/ITtcDbContext.cs`, add to the interface body:

```csharp
    DbSet<SyncStateEntity> SyncStates { get; set; }
```

- [ ] **Step 5: Add the DbSet and string conversion**

In `src/PongRank.DataAccess/TtcDbContext.cs`, add the property next to the other `DbSet`s:

```csharp
    public DbSet<SyncStateEntity> SyncStates { get; set; }
```

And inside `OnModelCreating`, after the `MatchEntity` block, add:

```csharp
        modelBuilder.Entity<SyncStateEntity>()
            .Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj --filter SyncStateEntityTests`
Expected: PASS.

- [ ] **Step 7: Generate the migration**

Run:
```bash
cd src/PongRank.DataAccess
dotnet ef migrations add SyncState
cd ../..
```
Expected: a new `Migrations/<timestamp>_SyncState.cs` creating table `SyncState` with a unique index on `(Competition, Year)`. Open it and confirm `Status` is `character varying(20)` and the index `IsUnique`.

- [ ] **Step 8: Build to confirm the migration compiles**

Run: `dotnet build PongRank.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 9: Commit**

```bash
git add src/PongRank.DataEntities src/PongRank.DataAccess
git commit -m "feat: add SyncState entity + migration"
```

---

## Task 3: `SyncDecision` — the pure close/outcome rule

**Files:**
- Create: `src/PongRank.WebApi/Utilities/SyncDecision.cs`
- Test: `src/PongRank.Tests/SyncDecisionTests.cs`

- [ ] **Step 1: Write the failing tests**

`src/PongRank.Tests/SyncDecisionTests.cs`:

```csharp
using PongRank.DataEntities;
using PongRank.WebApi.Utilities;
using Xunit;

namespace PongRank.Tests;

public class SyncDecisionTests
{
    [Fact]
    public void CurrentYear_stays_pending_even_when_nothing_left()
    {
        var o = SyncDecision.Evaluate(year: 2025, currentYear: 2025, nothingLeft: true, quotaExceeded: false);
        Assert.Equal(SyncStatus.Pending, o.Status);
        Assert.Equal("Completed", o.LastOutcome);
    }

    [Fact]
    public void PastYear_closes_as_completed_when_nothing_left()
    {
        var o = SyncDecision.Evaluate(year: 2024, currentYear: 2025, nothingLeft: true, quotaExceeded: false);
        Assert.Equal(SyncStatus.Closed, o.Status);
        Assert.Equal("Completed", o.LastOutcome);
    }

    [Fact]
    public void PastYear_closes_incomplete_when_stragglers_remain()
    {
        var o = SyncDecision.Evaluate(year: 2024, currentYear: 2025, nothingLeft: false, quotaExceeded: false);
        Assert.Equal(SyncStatus.Closed, o.Status);
        Assert.Equal("Closed-incomplete", o.LastOutcome);
    }

    [Fact]
    public void QuotaExceeded_keeps_pending_for_any_year()
    {
        var past = SyncDecision.Evaluate(year: 2024, currentYear: 2025, nothingLeft: false, quotaExceeded: true);
        Assert.Equal(SyncStatus.Pending, past.Status);
        Assert.Equal("QuotaExceeded", past.LastOutcome);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj --filter SyncDecisionTests`
Expected: compile error — `SyncDecision` not found.

- [ ] **Step 3: Implement**

`src/PongRank.WebApi/Utilities/SyncDecision.cs`:

```csharp
using PongRank.DataEntities;

namespace PongRank.WebApi.Utilities;

/// <summary>Result of deciding what to record after a sync attempt of one (Competition, Year).</summary>
public record SyncOutcome(SyncStatus Status, string LastOutcome);

/// <summary>Pure rule for whether a (Competition, Year) is done and what to record.</summary>
public static class SyncDecision
{
    public static SyncOutcome Evaluate(int year, int currentYear, bool nothingLeft, bool quotaExceeded)
    {
        if (quotaExceeded)
            return new SyncOutcome(SyncStatus.Pending, "QuotaExceeded");

        // The current season keeps producing new matches, so it is never closed.
        if (year >= currentYear)
            return new SyncOutcome(SyncStatus.Pending, "Completed");

        return new SyncOutcome(SyncStatus.Closed, nothingLeft ? "Completed" : "Closed-incomplete");
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj --filter SyncDecisionTests`
Expected: 4 passing.

- [ ] **Step 5: Commit**

```bash
git add src/PongRank.WebApi/Utilities/SyncDecision.cs src/PongRank.Tests/SyncDecisionTests.cs
git commit -m "feat: add pure SyncDecision close/outcome rule"
```

---

## Task 4: `IFrenoyApiClient` + quota capture on `FrenoyApiClient`

**Files:**
- Create: `src/PongRank.Frenoy/IFrenoyApiClient.cs`
- Modify: `src/PongRank.Frenoy/FrenoyApiClient.cs`

- [ ] **Step 1: Define the abstraction**

`src/PongRank.Frenoy/IFrenoyApiClient.cs`:

```csharp
namespace PongRank.FrenoyApi;

/// <summary>The slice of <see cref="FrenoyApiClient"/> the sync jobs depend on (enables faking in tests).</summary>
public interface IFrenoyApiClient
{
    void Open(FrenoySettings settings);
    Task Sync();
    Task SyncMatches(string clubUniqueIndex);

    /// <summary>Frenoy quota seen on the most recent <see cref="GetQuotaAsync"/> call, or null if unknown.</summary>
    (int Current, int Allowed)? LastQuota { get; }

    /// <summary>Best-effort Test call to read the API quota. Returns null on any failure.</summary>
    Task<(int Current, int Allowed)?> GetQuotaAsync();
}
```

- [ ] **Step 2: Implement on `FrenoyApiClient`**

In `src/PongRank.Frenoy/FrenoyApiClient.cs`, change the class declaration:

```csharp
public class FrenoyApiClient : IFrenoyApiClient
```

Add a backing field near the other fields:

```csharp
    private (int Current, int Allowed)? _lastQuota;
```

Add the property + method (place after `Open(...)`):

```csharp
    public (int Current, int Allowed)? LastQuota => _lastQuota;

    public async Task<(int Current, int Allowed)?> GetQuotaAsync()
    {
        try
        {
            var response = await _frenoy.TestAsync(new TestRequest1(new TestRequest()));
            var test = response.TestResponse;
            if (int.TryParse(test.CurrentQuota, out int current) && int.TryParse(test.AllowedQuota, out int allowed))
            {
                _lastQuota = (current, allowed);
                return _lastQuota;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("GetQuota failed: {ErrorMessage}", ex.Message);
        }

        return null;
    }
```

> Note: `TestRequest`/`TestRequest1`/`TestResponse`/`CurrentQuota`/`AllowedQuota` are generated in `Connected Services/FrenoyVttl/Reference.cs`. `CurrentQuota`/`AllowedQuota` are `string`. The `Test` op is `TestAsync(TestRequest1)` returning `TestResponse1` whose `.TestResponse` carries the quota fields.

- [ ] **Step 3: Build**

Run: `dotnet build PongRank.slnx`
Expected: Build succeeded, 0 warnings. (No unit test here — `FrenoyApiClient` wraps an external SOAP service and is out of scope for tests; the runner is tested against the `IFrenoyApiClient` fake.)

- [ ] **Step 4: Commit**

```bash
git add src/PongRank.Frenoy/IFrenoyApiClient.cs src/PongRank.Frenoy/FrenoyApiClient.cs
git commit -m "feat: add IFrenoyApiClient + quota read via Test call"
```

---

## Task 5: `HistoricalSyncRunner` (skip/close/quota logic + logging)

**Files:**
- Create: `src/PongRank.WebApi/Utilities/HistoricalSyncRunner.cs`
- Create: `src/PongRank.Tests/Fakes/FakeFrenoyApiClient.cs`
- Test: `src/PongRank.Tests/HistoricalSyncRunnerTests.cs`

- [ ] **Step 1: Create the fake client**

`src/PongRank.Tests/Fakes/FakeFrenoyApiClient.cs`:

```csharp
using PongRank.FrenoyApi;

namespace PongRank.Tests.Fakes;

/// <summary>Records Sync() calls and optionally throws to simulate quota/errors.</summary>
internal class FakeFrenoyApiClient : IFrenoyApiClient
{
    public List<(int Year, string Competition)> SyncedYears { get; } = new();
    public Func<FrenoySettings, Task>? OnSync { get; set; }
    public Exception? ThrowOnSync { get; set; }

    private FrenoySettings _settings = new();

    public void Open(FrenoySettings settings) => _settings = settings;

    public async Task Sync()
    {
        SyncedYears.Add((_settings.Year, _settings.Competition.ToString()));
        if (ThrowOnSync is not null) throw ThrowOnSync;
        if (OnSync is not null) await OnSync(_settings);
    }

    public Task SyncMatches(string clubUniqueIndex) => Task.CompletedTask;

    public (int Current, int Allowed)? LastQuota => (100, 200);
    public Task<(int Current, int Allowed)?> GetQuotaAsync() => Task.FromResult<(int, int)?>((100, 200));
}
```

- [ ] **Step 2: Write the failing tests**

`src/PongRank.Tests/HistoricalSyncRunnerTests.cs`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using PongRank.DataEntities;
using PongRank.Model;
using PongRank.Tests.Fakes;
using PongRank.WebApi.Utilities;
using Xunit;

namespace PongRank.Tests;

public class HistoricalSyncRunnerTests
{
    // Fixed "now" → CurrentYear = 2025 (June < September → year - 1).
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero));

    private static SyncJobSettings Settings(params int[] years) => new()
    {
        SyncYears = years,
        SyncCompetitions = new[] { Competition.Vttl },
        SyncCategoryNames = new[] { "Oost-Vlaanderen" },
    };

    private static HistoricalSyncRunner Runner(TtcDbContext db, FakeFrenoyApiClient frenoy, SyncJobSettings settings) =>
        new(db, frenoy, settings, Clock, NullLogger<HistoricalSyncRunner>.Instance);

    [Fact]
    public async Task Closed_year_is_skipped_no_api_call()
    {
        using var db = InMemoryDb.Create();
        db.SyncStates.Add(new SyncStateEntity { Competition = Competition.Vttl, Year = 2020, Status = SyncStatus.Closed });
        await db.SaveChangesAsync();
        var frenoy = new FakeFrenoyApiClient();

        await Runner(db, frenoy, Settings(2020)).RunAsync();

        Assert.Empty(frenoy.SyncedYears);
    }

    [Fact]
    public async Task PastYear_closes_after_clean_pass()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient();

        await Runner(db, frenoy, Settings(2024)).RunAsync();

        var state = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Closed, state.Status);
        Assert.Equal(1, state.AttemptCount);
        Assert.Single(frenoy.SyncedYears);
    }

    [Fact]
    public async Task CurrentYear_stays_pending()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient();

        await Runner(db, frenoy, Settings(2025)).RunAsync();

        var state = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Pending, state.Status);
    }

    [Fact]
    public async Task QuotaExceeded_keeps_year_pending_and_stops_the_run()
    {
        using var db = InMemoryDb.Create();
        var frenoy = new FakeFrenoyApiClient { ThrowOnSync = new Exception("Quota exceeded for today") };

        await Runner(db, frenoy, Settings(2023, 2022)).RunAsync();

        // Only the first year was attempted; the run stopped on quota.
        Assert.Single(frenoy.SyncedYears);
        var state = Assert.Single(db.SyncStates);
        Assert.Equal(SyncStatus.Pending, state.Status);
        Assert.Equal("QuotaExceeded", state.LastOutcome);
    }
}
```

Also create a minimal fake clock `src/PongRank.Tests/Fakes/FakeTimeProvider.cs`:

```csharp
namespace PongRank.Tests.Fakes;

internal class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;
    public FakeTimeProvider(DateTimeOffset now) => _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
}
```

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj --filter HistoricalSyncRunnerTests`
Expected: compile error — `HistoricalSyncRunner` not found.

- [ ] **Step 4: Implement the runner**

`src/PongRank.WebApi/Utilities/HistoricalSyncRunner.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using PongRank.DataEntities;
using PongRank.DataEntities.Core;
using PongRank.FrenoyApi;
using PongRank.Model;

namespace PongRank.WebApi.Utilities;

/// <summary>
/// Historical sync loop: walks SyncCompetitions × SyncYears, skipping (Competition, Year)
/// pairs already Closed, closing past years after a clean pass, never closing the current
/// year, and stopping the whole run when Frenoy quota is exhausted.
/// </summary>
public class HistoricalSyncRunner
{
    private readonly ITtcDbContext _db;
    private readonly IFrenoyApiClient _frenoy;
    private readonly SyncJobSettings _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger<HistoricalSyncRunner> _logger;

    public HistoricalSyncRunner(
        ITtcDbContext db,
        IFrenoyApiClient frenoy,
        SyncJobSettings settings,
        TimeProvider clock,
        ILogger<HistoricalSyncRunner> logger)
    {
        _db = db;
        _frenoy = frenoy;
        _settings = settings;
        _clock = clock;
        _logger = logger;
    }

    // Season spans Aug–Jul: before September, "current" is the previous calendar year.
    private int CurrentYear
    {
        get
        {
            var now = _clock.GetUtcNow();
            return now.Month < 9 ? now.Year - 1 : now.Year;
        }
    }

    public async Task RunAsync()
    {
        int currentYear = CurrentYear;

        foreach (var competition in _settings.SyncCompetitions)
        {
            await LogQuotaAsync(competition, currentYear);

            foreach (int year in _settings.SyncYears)
            {
                var state = await GetOrCreateState(competition, year);
                if (state.Status == SyncStatus.Closed)
                {
                    _logger.LogInformation("SyncRun {Competition} {Year} skipped (Closed)", competition, year);
                    continue;
                }

                try
                {
                    _frenoy.Open(new FrenoySettings(competition, year, _settings.SyncCategoryNames));

                    int matchesBefore = await CountMatches(competition, year);
                    await _frenoy.Sync();
                    int matchesAfter = await CountMatches(competition, year);

                    var (clubsSynced, clubsTotal) = await CountClubs(competition, year);
                    var (tournamentsSynced, tournamentsTotal) = await CountTournaments(competition, year);
                    bool nothingLeft = clubsSynced == clubsTotal && tournamentsSynced == tournamentsTotal;

                    var outcome = SyncDecision.Evaluate(year, currentYear, nothingLeft, quotaExceeded: false);
                    Apply(state, outcome);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation(
                        "SyncRun {Competition} {Year} outcome={Outcome} clubsSynced={ClubsSynced}/{ClubsTotal} matchesAdded={MatchesAdded} tournamentsSynced={TournamentsSynced}/{TournamentsTotal}",
                        competition, year, outcome.LastOutcome, clubsSynced, clubsTotal,
                        matchesAfter - matchesBefore, tournamentsSynced, tournamentsTotal);
                }
                catch (Exception ex) when (ex.Message.Contains("Quota exceeded"))
                {
                    Apply(state, new SyncOutcome(SyncStatus.Pending, "QuotaExceeded"));
                    await _db.SaveChangesAsync();
                    _logger.LogWarning("SyncRun {Competition} {Year} outcome=QuotaExceeded", competition, year);
                    return; // quota is global — stop the whole run
                }
                catch (Exception ex)
                {
                    Apply(state, new SyncOutcome(state.Status, "Error"));
                    await _db.SaveChangesAsync();
                    _logger.LogError(ex, "SyncRun {Competition} {Year} outcome=Error {ErrorMessage}", competition, year, ex.Message);
                }
            }
        }
    }

    private void Apply(SyncStateEntity state, SyncOutcome outcome)
    {
        state.Status = outcome.Status;
        state.LastOutcome = outcome.LastOutcome;
        state.AttemptCount++;
        state.LastAttemptUtc = _clock.GetUtcNow().UtcDateTime;
    }

    private async Task LogQuotaAsync(Competition competition, int currentYear)
    {
        // Open a client so GetQuota targets the right competition endpoint.
        _frenoy.Open(new FrenoySettings(competition, currentYear, _settings.SyncCategoryNames));
        var quota = await _frenoy.GetQuotaAsync();
        if (quota is { } q)
            _logger.LogInformation("SyncQuota {Competition} current={QuotaCurrent} allowed={QuotaAllowed}", competition, q.Current, q.Allowed);
    }

    private async Task<SyncStateEntity> GetOrCreateState(Competition competition, int year)
    {
        var state = await _db.SyncStates
            .SingleOrDefaultAsync(x => x.Competition == competition && x.Year == year);
        if (state is null)
        {
            state = new SyncStateEntity { Competition = competition, Year = year, Status = SyncStatus.Pending };
            _db.SyncStates.Add(state);
        }
        return state;
    }

    private Task<int> CountMatches(Competition competition, int year) =>
        _db.Matches.CountAsync(x => x.Competition == competition && x.Year == year);

    private async Task<(int Synced, int Total)> CountClubs(Competition competition, int year)
    {
        var query = _db.Clubs.Where(x => x.Competition == competition && x.Year == year);
        if (_settings.SyncCategoryNames.Length > 0)
            query = query.Where(x => _settings.SyncCategoryNames.Contains(x.CategoryName));
        int total = await query.CountAsync();
        int synced = await query.CountAsync(x => x.SyncCompleted);
        return (synced, total);
    }

    private async Task<(int Synced, int Total)> CountTournaments(Competition competition, int year)
    {
        var query = _db.Tournaments.Where(x => x.Competition == competition && x.Year == year);
        int total = await query.CountAsync();
        int synced = await query.CountAsync(x => x.SyncCompleted);
        return (synced, total);
    }
}
```

> `ILogger` lives in `Microsoft.Extensions.Logging` — already available via the WebApi project's implicit usings; if the build flags it, add `using Microsoft.Extensions.Logging;`.

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj --filter HistoricalSyncRunnerTests`
Expected: 4 passing.

- [ ] **Step 6: Commit**

```bash
git add src/PongRank.WebApi/Utilities/HistoricalSyncRunner.cs src/PongRank.Tests/Fakes src/PongRank.Tests/HistoricalSyncRunnerTests.cs
git commit -m "feat: add HistoricalSyncRunner with skip/close/quota logic"
```

---

## Task 6: Wire `FrenoySyncJob` to the runner + DI registration

**Files:**
- Modify: `src/PongRank.WebApi/Utilities/FrenoySyncJob.cs`, `src/PongRank.WebApi/Program.cs`

- [ ] **Step 1: Replace the loop in `FrenoySyncJob` with a runner call**

Replace the body of `SyncMatches()` in `src/PongRank.WebApi/Utilities/FrenoySyncJob.cs` with:

```csharp
    private async Task SyncMatches()
    {
        using var scope = _services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FrenoySyncJob>>();
        try
        {
            logger.LogInformation("SyncJob Started at {SyncStart}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            var runner = scope.ServiceProvider.GetRequiredService<HistoricalSyncRunner>();
            await runner.RunAsync();
            logger.LogInformation("SyncJob Ended at {SyncEnd}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FrenoySyncJob failed {ErrorMessage}", ex.Message);
        }
        finally
        {
            _timer?.Change(TimeSpan.FromHours(12), Timeout.InfiniteTimeSpan);
        }
    }
```

> Quota/error handling now lives in the runner; the job only reschedules. The old inner loop over `SyncCompetitions`/`SyncYears` and the `"Quota exceeded"` branch are removed.

- [ ] **Step 2: Register the new services in `Program.cs`**

In `src/PongRank.WebApi/Program.cs`, in the service-registration block (near `AddScoped<FrenoyApiClient>()`), add:

```csharp
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<IFrenoyApiClient>(sp => sp.GetRequiredService<FrenoyApiClient>());
    builder.Services.AddScoped<HistoricalSyncRunner>();
```

(Keep the existing `builder.Services.AddScoped<FrenoyApiClient>();` — `CurrentSeasonSyncJob` still resolves the concrete type.)

- [ ] **Step 3: Build**

Run: `dotnet build PongRank.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 4: Run the full test suite**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj`
Expected: all green.

- [ ] **Step 5: Commit**

```bash
git add src/PongRank.WebApi/Utilities/FrenoySyncJob.cs src/PongRank.WebApi/Program.cs
git commit -m "refactor: delegate FrenoySyncJob to HistoricalSyncRunner"
```

---

## Task 7: `GET /api/SyncStatus`

**Files:**
- Create: `src/PongRank.WebApi/Controllers/SyncStatusController.cs`
- Test: `src/PongRank.Tests/SyncStatusControllerTests.cs`

- [ ] **Step 1: Write the failing test**

`src/PongRank.Tests/SyncStatusControllerTests.cs`:

```csharp
using PongRank.DataEntities;
using PongRank.Model;
using PongRank.Tests.Fakes;
using PongRank.WebApi.Controllers;
using Xunit;

namespace PongRank.Tests;

public class SyncStatusControllerTests
{
    [Fact]
    public async Task Returns_status_with_club_and_tournament_counts()
    {
        using var db = InMemoryDb.Create();
        db.SyncStates.Add(new SyncStateEntity { Competition = Competition.Vttl, Year = 2024, Status = SyncStatus.Closed, LastOutcome = "Completed", AttemptCount = 1 });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Oost-Vlaanderen", SyncCompleted = true });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Oost-Vlaanderen", SyncCompleted = false });
        db.Tournaments.Add(new TournamentEntity { Competition = Competition.Vttl, Year = 2024, SyncCompleted = true });
        await db.SaveChangesAsync();

        var result = await new SyncStatusController(db).Get(category: null);

        var row = Assert.Single(result);
        Assert.Equal("Vttl", row.Competition);
        Assert.Equal(2024, row.Year);
        Assert.Equal("Closed", row.Status);
        var cat = Assert.Single(row.Categories);
        Assert.Equal("Oost-Vlaanderen", cat.CategoryName);
        Assert.Equal(1, cat.ClubsSynced);
        Assert.Equal(1, cat.ClubsPending);
        Assert.Equal(1, row.Tournaments.Synced);
        Assert.Equal(0, row.Tournaments.Pending);
    }

    [Fact]
    public async Task Category_filter_limits_club_rows()
    {
        using var db = InMemoryDb.Create();
        db.SyncStates.Add(new SyncStateEntity { Competition = Competition.Vttl, Year = 2024, Status = SyncStatus.Pending });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Oost-Vlaanderen", SyncCompleted = true });
        db.Clubs.Add(new ClubEntity { Competition = Competition.Vttl, Year = 2024, CategoryName = "Antwerpen", SyncCompleted = true });
        await db.SaveChangesAsync();

        var result = await new SyncStatusController(db).Get(category: "Oost-Vlaanderen");

        var row = Assert.Single(result);
        var cat = Assert.Single(row.Categories);
        Assert.Equal("Oost-Vlaanderen", cat.CategoryName);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj --filter SyncStatusControllerTests`
Expected: compile error — `SyncStatusController` not found.

- [ ] **Step 3: Implement the controller + DTOs**

`src/PongRank.WebApi/Controllers/SyncStatusController.cs`:

```csharp
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
```

> The InMemory provider runs these `GroupBy` projections client-side; Npgsql translates them server-side. Both are supported. The `category == null` guard short-circuits the filter.

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test src/PongRank.Tests/PongRank.Tests.csproj --filter SyncStatusControllerTests`
Expected: 2 passing.

- [ ] **Step 5: Build (zero warnings) and full suite**

Run: `dotnet build PongRank.slnx && dotnet test src/PongRank.Tests/PongRank.Tests.csproj`
Expected: Build 0 warnings; all tests green.

- [ ] **Step 6: Commit**

```bash
git add src/PongRank.WebApi/Controllers/SyncStatusController.cs src/PongRank.Tests/SyncStatusControllerTests.cs
git commit -m "feat: add GET /api/SyncStatus endpoint"
```

---

## Task 8: Grafana dashboard (Home repo)

**Files:**
- Create: `homelab/obs-01/grafana/provisioning/dashboards/06-ttc-pong-sync.json`
  (in the **Home** repo: `/mnt/c/Users/woute/Dropbox/Personal/Programming/UnixCode/_personal/Home`)

> This is a separate git repo. Commit there, not in pong-rank.

- [ ] **Step 1: Confirm the Loki label for this app**

The pong app logs to Loki via Itenium.Forge (`service=ttc-ml`, `application=ttc`) — **not** via the Coolify container collector used by `05-ttc-tabtapi.json`. Before finalizing the selector, open Grafana → Explore → Loki → Label browser and confirm which label/value selects this app (likely `service="ttc-ml"` or `application="ttc"`). Use the confirmed selector below in place of `{service="ttc-ml"}`.

- [ ] **Step 2: Create the dashboard**

`homelab/obs-01/grafana/provisioning/dashboards/06-ttc-pong-sync.json`:

```json
{
  "uid": "homelab-ttc-pong-sync",
  "title": "TTC PongRank Sync",
  "tags": ["homelab", "loki", "ttc", "pong"],
  "timezone": "browser",
  "schemaVersion": 39,
  "refresh": "1m",
  "time": { "from": "now-7d", "to": "now" },
  "templating": {
    "list": [
      {
        "name": "selector",
        "label": "Loki selector for the pong app",
        "type": "constant",
        "query": "service=\"ttc-ml\"",
        "current": { "text": "service=\"ttc-ml\"", "value": "service=\"ttc-ml\"" },
        "hide": 0
      }
    ]
  },
  "panels": [
    {
      "type": "row",
      "title": "PongRank sync (from Serilog → Loki)",
      "gridPos": { "h": 1, "w": 24, "x": 0, "y": 0 },
      "collapsed": false,
      "panels": []
    },
    {
      "type": "stat",
      "title": "Sync runs (selected range)",
      "datasource": { "type": "loki", "uid": "loki" },
      "gridPos": { "h": 6, "w": 6, "x": 0, "y": 1 },
      "fieldConfig": { "defaults": { "unit": "short", "color": { "mode": "thresholds" }, "thresholds": { "mode": "absolute", "steps": [ { "color": "blue", "value": null } ] } }, "overrides": [] },
      "options": { "reduceOptions": { "calcs": ["lastNotNull"], "fields": "", "values": false }, "textMode": "auto", "colorMode": "value", "graphMode": "area", "justifyMode": "auto" },
      "targets": [
        {
          "refId": "A",
          "datasource": { "type": "loki", "uid": "loki" },
          "editorMode": "code",
          "queryType": "instant",
          "expr": "sum(count_over_time({$selector} |= \"SyncRun\" != \"skipped\" [$__range]))"
        }
      ]
    },
    {
      "type": "timeseries",
      "title": "Sync outcomes over time",
      "datasource": { "type": "loki", "uid": "loki" },
      "gridPos": { "h": 6, "w": 18, "x": 6, "y": 1 },
      "fieldConfig": { "defaults": { "unit": "short", "min": 0, "custom": { "drawStyle": "bars", "fillOpacity": 60, "stacking": { "mode": "normal" } } }, "overrides": [] },
      "options": { "legend": { "displayMode": "table", "placement": "right", "calcs": ["sum"] }, "tooltip": { "mode": "multi", "sort": "desc" } },
      "targets": [
        {
          "refId": "A",
          "datasource": { "type": "loki", "uid": "loki" },
          "editorMode": "code",
          "expr": "sum by (outcome) (count_over_time({$selector} |= \"SyncRun\" | regexp \"outcome=(?P<outcome>[A-Za-z-]+)\" [$__auto]))",
          "legendFormat": "{{outcome}}"
        }
      ]
    },
    {
      "type": "timeseries",
      "title": "Frenoy quota remaining (allowed - current)",
      "datasource": { "type": "loki", "uid": "loki" },
      "gridPos": { "h": 7, "w": 12, "x": 0, "y": 7 },
      "fieldConfig": { "defaults": { "unit": "short", "min": 0, "custom": { "drawStyle": "line", "fillOpacity": 10 } }, "overrides": [] },
      "options": { "legend": { "displayMode": "list", "placement": "bottom" }, "tooltip": { "mode": "multi" } },
      "targets": [
        {
          "refId": "A",
          "datasource": { "type": "loki", "uid": "loki" },
          "editorMode": "code",
          "expr": "max by (competition) (last_over_time({$selector} |= \"SyncQuota\" | regexp \"SyncQuota (?P<competition>[A-Za-z]+) current=(?P<current>[0-9]+) allowed=(?P<allowed>[0-9]+)\" | unwrap current [$__auto]))",
          "legendFormat": "{{competition}} current"
        }
      ]
    },
    {
      "type": "table",
      "title": "Latest run per Competition/Year",
      "datasource": { "type": "loki", "uid": "loki" },
      "gridPos": { "h": 7, "w": 12, "x": 12, "y": 7 },
      "fieldConfig": { "defaults": { "custom": { "align": "auto" } }, "overrides": [] },
      "options": { "showHeader": true, "sortBy": [ { "displayName": "Value", "desc": true } ] },
      "transformations": [
        { "id": "labelsToFields", "options": {} },
        { "id": "organize", "options": { "excludeByName": { "Time": true }, "renameByName": { "competition": "Competition", "year": "Year", "outcome": "Outcome", "Value": "Runs" } } }
      ],
      "targets": [
        {
          "refId": "A",
          "datasource": { "type": "loki", "uid": "loki" },
          "editorMode": "code",
          "queryType": "instant",
          "expr": "sum by (competition, year, outcome) (count_over_time({$selector} |= \"SyncRun\" | regexp \"SyncRun (?P<competition>[A-Za-z]+) (?P<year>[0-9]+) outcome=(?P<outcome>[A-Za-z-]+)\" [$__range]))"
        }
      ]
    },
    {
      "type": "logs",
      "title": "Sync log (SyncRun / SyncQuota / errors)",
      "datasource": { "type": "loki", "uid": "loki" },
      "gridPos": { "h": 10, "w": 24, "x": 0, "y": 14 },
      "options": { "showTime": true, "showCommonLabels": false, "wrapLogMessage": true, "enableLogDetails": true, "dedupStrategy": "none", "sortOrder": "Descending" },
      "targets": [
        {
          "refId": "A",
          "datasource": { "type": "loki", "uid": "loki" },
          "editorMode": "code",
          "expr": "{$selector} |~ \"SyncRun|SyncQuota|FrenoySyncJob failed\"",
          "maxLines": 1000
        }
      ]
    }
  ]
}
```

- [ ] **Step 3: Validate JSON**

Run: `cat homelab/obs-01/grafana/provisioning/dashboards/06-ttc-pong-sync.json | python3 -m json.tool > /dev/null && echo OK`
Expected: `OK`.

- [ ] **Step 4: Commit (Home repo)**

```bash
cd /mnt/c/Users/woute/Dropbox/Personal/Programming/UnixCode/_personal/Home
git add homelab/obs-01/grafana/provisioning/dashboards/06-ttc-pong-sync.json
git commit -m "feat(grafana): add TTC PongRank sync dashboard"
```

---

## Task 9: Final verification

- [ ] **Step 1: Full build + test**

Run: `dotnet build PongRank.slnx && dotnet test src/PongRank.Tests/PongRank.Tests.csproj`
Expected: Build 0 warnings; all tests green.

- [ ] **Step 2: Manual smoke (optional, needs DB + quota)**

Bring up the stack (`docker compose up -d --build`), hit `GET /api/SyncStatus`, confirm JSON rows appear after the migration runs at startup. Confirm `SyncRun`/`SyncQuota` lines arrive in Loki and the dashboard renders (adjust `$selector` if empty).

- [ ] **Step 3: Confirm the dashboard selector**

If the dashboard panels are empty, fix the `selector` constant to the label confirmed in Task 8 Step 1 and re-commit in the Home repo.

---

## Self-Review Notes

- **Spec coverage:** §1 SyncState → Task 2; §2 runner/decision → Tasks 3,5,6; §3 endpoint → Task 7; §4 logging+Grafana → Tasks 5 (log lines), 8 (dashboard); §5 testing → Tasks 1,3,5,7. Quota-via-Test → Task 4.
- **Type consistency:** `SyncDecision.Evaluate(int, int, bool, bool)` and `SyncOutcome(Status, LastOutcome)` used identically in Tasks 3 and 5. `IFrenoyApiClient` members in Task 4 match the fake (Task 5) and runner calls. `SyncStatusRow`/`CategoryCount`/`SyncedPending` defined and consumed in Task 7 only.
- **Outcome strings** are exactly `Completed` | `Closed-incomplete` | `QuotaExceeded` | `Error` everywhere (entity comment, decision, runner, dashboard regex `[A-Za-z-]+`).
