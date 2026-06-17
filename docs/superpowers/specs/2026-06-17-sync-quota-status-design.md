# Sync Quota Control + Status — Design

**Date:** 2026-06-17
**Repos touched:** `pong-rank` (code), `Home` (Grafana dashboard)

## Problem

The historical `FrenoySyncJob` runs every 12h, looping `SyncCompetitions × SyncYears`
(currently 2005–2025) and calling `frenoy.Sync()` for each pair. `Sync()` short-circuits
cheaply where clubs/players already exist, but **`SyncMatches` re-hits the Frenoy API for
every club whose `SyncCompleted == false`**, and `SyncTournaments` re-fetches the tournament
list whenever a year stored 0 tournaments. A club/tournament that never reaches
`SyncCompleted` (data quirk, never-played match) therefore burns the quota-limited Frenoy
API **every cycle, forever**.

There is no per-`(Competition, Year)` "this year is done, stop touching it" concept — only
per-club and per-tournament `SyncCompleted` bools. We also have no way to inspect sync
state without running ad-hoc SQL, and no Grafana view of what each sync did.

## Goals

1. Stop spending Frenoy quota on years that can no longer yield new data.
2. An HTTP endpoint to inspect sync status (productize the existing SQL query).
3. A Grafana dashboard (homelab `Home` repo) showing what happened per sync.

## Eligibility rule

`CurrentYear = Now.Month < 9 ? Now.Year - 1 : Now.Year` (season spans Aug–Jul; today → **2025**).

| Year                 | Behavior                                                                 |
|----------------------|--------------------------------------------------------------------------|
| `== CurrentYear`     | **Never closed.** Re-attempt every cycle — new matches always arriving.  |
| `< CurrentYear`      | **Close after one clean pass** (Sync finished without a quota abort), even if some clubs never completed. |
| any, quota-aborted   | Stay open, retry next cycle. Quota exhausted → stop the whole run early.  |

`Closed` = never call the Frenoy API for that pair again. Past-season data is frozen, so
"gave up with stragglers" is a legitimate terminal state; the existing
`GROUP BY … SyncCompleted` query still shows true per-club partial state.

## 1. State model — `SyncState` table

New EF entity `SyncStateEntity` → table `"SyncState"`, one row per `(Competition, Year)`:

| Column           | Type          | Notes                                                        |
|------------------|---------------|--------------------------------------------------------------|
| `Id`             | int PK        |                                                              |
| `Competition`    | varchar(10)   | enum, same convention as `Clubs`/`Tournaments`               |
| `Year`           | int           | unique index `(Competition, Year)`                           |
| `Status`         | varchar       | `Pending` \| `Closed`                                        |
| `LastAttemptUtc` | timestamptz?  | nullable until first attempt                                 |
| `AttemptCount`   | int           |                                                              |
| `LastOutcome`    | varchar       | `Completed` \| `Closed-incomplete` \| `QuotaExceeded` \| `Error` |

New migration + `DbSet<SyncStateEntity> SyncStates` on `TtcDbContext` and `ITtcDbContext`.

## 2. Sync job logic — extract `HistoricalSyncRunner`

`FrenoySyncJob` currently mixes `Timer` plumbing with the loop, making the rule untestable.
Pull the loop into `HistoricalSyncRunner.RunAsync()` (DI-injected, no Timer); `FrenoySyncJob`
becomes the thin Timer wrapper that calls it (preserving the 12h reschedule on
success/quota/error).

Per `(competition, year)`:

```
currentYear = Now.Month < 9 ? Now.Year - 1 : Now.Year
state = GetOrCreate(competition, year)              // new rows start Pending
if state.Status == Closed:  log skip; continue      // ← quota saver: zero API calls
try:
    frenoy.Open(settings); await frenoy.Sync()
    state.AttemptCount++; state.LastAttemptUtc = now
    nothingLeft = no clubs/tournaments for (comp,year) with SyncCompleted == false
    if year < currentYear:
        state.Status = Closed
        state.LastOutcome = nothingLeft ? "Completed" : "Closed-incomplete"
    else:
        state.LastOutcome = "Completed"             // CurrentYear stays Pending → re-attempted
    emit SyncRun summary log (see §4); SaveChanges
catch quota-exceeded:
    state.LastOutcome = "QuotaExceeded"; AttemptCount++; SaveChanges; emit log
    break out of BOTH loops                         // quota is global — stop wasting the run
catch other:
    state.LastOutcome = "Error"; log error; continue
```

The terminal decision is a **pure method** (`year`, `currentYear`, `nothingLeft`, `quota?`
→ `{ close, outcome }`) so the rule is unit-tested in isolation. Quota detection reuses the
existing `ex.Message.Contains("Quota exceeded")` check.

## 3. Status endpoint — `GET /api/SyncStatus`

New `SyncStatusController` (Forge auto-discovers it). `SyncState` joined with club/tournament
`SyncCompleted` counts — the SQL query as JSON. Optional `?category=Oost-Vlaanderen` filter.

```json
[
  { "competition": "Vttl", "year": 2024, "status": "Closed",
    "lastAttemptUtc": "2026-06-17T03:00:00Z", "attempts": 1, "lastOutcome": "Completed",
    "categories": [ { "categoryName": "Oost-Vlaanderen", "clubsSynced": 40, "clubsPending": 2 } ],
    "tournaments": { "synced": 10, "pending": 0 } }
]
```

Tournaments have no `CategoryName`, so they are reported per `(Competition, Year)`, not per
category. This is the DB-truth "current state" view, complementing the Loki history dashboard.

## 4. Structured logging + Grafana (Loki)

One summary line per year-attempt, structured for LogQL parsing (mirrors how
`05-ttc-tabtapi.json` parses `Executing endpoint`):

```
logger.LogInformation(
  "SyncRun {Competition} {Year} outcome={Outcome} clubsSynced={ClubsSynced}/{ClubsTotal} " +
  "matchesAdded={MatchesAdded} tournamentsSynced={TournamentsSynced}/{TournamentsTotal} " +
  "quota={QuotaCurrent}/{QuotaAllowed}", …)
```

- `matchesAdded` = match-count delta (count matches for `(comp,year)` before/after `Sync()`;
  no change to `Sync()`'s signature).
- Quota: the SOAP proxy exposes `CurrentQuota`/`AllowedQuota` **only on `TestResponse`** (not
  on match/club responses). So `FrenoyApiClient` gets a `GetQuotaAsync()` that issues a `Test`
  call; the runner logs quota once per competition per cycle (best-effort, wrapped so a failed
  `Test` never aborts a sync). Powers the "quota remaining" panel and the don't-blow-quota goal.
- New dashboard `homelab/obs-01/grafana/provisioning/dashboards/06-ttc-pong-sync.json`
  (**Home** repo). Panels: sync runs over time by comp/year, outcome breakdown, quota
  remaining, errors, last-run table.
- ⚠️ Loki selector is **not** `{container=…}` like tabtapi — this app pushes via Forge under
  `service=ttc-ml` / `application=ttc`. Set the selector as a dashboard variable and confirm
  the exact label against Loki's label browser at deploy time.

## 5. Testing (TDD)

- `HistoricalSyncRunner` with fake `FrenoyApiClient` + in-memory/SQLite `TtcDbContext`:
  closed years skipped (0 API calls); past year closes after clean pass; current year stays
  `Pending`; quota abort breaks the loop and leaves `Pending`.
- Decision method: pure unit tests over the year/quota matrix.
- `SyncStatusController`: seeded DB → projection shape/counts.
- Grafana JSON: parse-validates; no behavioral test.

## Out of scope

- `CurrentSeasonSyncJob` (separate weekly targeted current-season sync) — unchanged.
- `SyncTournaments` re-fetching an empty tournament list for the current year (1 call/cycle) —
  acceptable; closed years skip it entirely.
- FrenoyApiClient SOAP integration tests (external dependency).
