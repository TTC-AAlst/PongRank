using System;
using System.Diagnostics;
using FrenoyVttl;
using Microsoft.EntityFrameworkCore;
using PongRank.DataEntities;
using PongRank.DataEntities.Core;
using PongRank.Model;
using PongRank.Model.Core;

namespace PongRank.FrenoyApi;

public class FrenoyApiClient
{
    #region Fields
    private const string FrenoyVttlEndpoint = "https://api.vttl.be/index.php?s=vttl";
    private const string FrenoySportaEndpoint = "https://ttonline.sporta.be/api/index.php?s=sporcrea";
    private static readonly TimeZoneInfo BelgianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");

    private FrenoySettings _settings;
    private TabTAPI_PortTypeClient _frenoy;
    private readonly ITtcDbContext _db;
    private readonly TtcLogger _logger;
    #endregion

    #region Constructor
    public FrenoyApiClient(ITtcDbContext ttcDbContext, TtcLogger logger)
    {
        _db = ttcDbContext;
        _logger = logger;
        _settings = new FrenoySettings(Competition.Vttl, DateTime.Now.Year, ["OostVlaanderen"]);
        _frenoy = new TabTAPI_PortTypeClient();
    }

    public void Open(FrenoySettings settings)
    {
        _settings = settings;

        var binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport)
        {
            MaxReceivedMessageSize = 20000000,
            MaxBufferSize = 20000000,
            MaxBufferPoolSize = 20000000,
            AllowCookies = true
        };

        if (settings.Competition == Competition.Vttl)
        {
            _frenoy = new FrenoyVttl.TabTAPI_PortTypeClient(
                binding,
                new System.ServiceModel.EndpointAddress(new Uri(FrenoyVttlEndpoint))
            );
        }
        else
        {
            Debug.Assert(settings.Competition == Competition.Sporta);
            _frenoy = new TabTAPI_PortTypeClient(
                binding,
                new System.ServiceModel.EndpointAddress(new Uri(FrenoySportaEndpoint))
            );
        }
    }
    #endregion

    public async Task Sync()
    {
        List<ClubEntity> clubs = await SyncClubs();
        await SyncPlayers(clubs);
        await SyncMatches(clubs);

        // await SyncTournaments();
    }

    private async Task SyncMatches(List<ClubEntity> clubs)
    {
        var toSyncClubs = clubs.Where(x => !x.SyncCompleted).ToArray();
        if (_settings.CategoryNames.Length > 0)
            toSyncClubs = [..toSyncClubs.Where(x => _settings.CategoryNames.Contains(x.CategoryName))];

        var matchUniqueIds = await _db.Matches
            .Where(x => x.Competition == _settings.Competition)
            .Where(x => x.Year == _settings.Year)
            .Select(x => x.MatchUniqueId)
            .ToListAsync();

        foreach (var club in toSyncClubs)
        {
            Debug.Assert(club.Competition == _settings.Competition);
            Debug.Assert(club.Year == _settings.Year);

            var matchesResponse = await _frenoy.GetMatchesAsync(new GetMatchesRequest1(new GetMatchesRequest()
            {
                Season = _settings.FrenoySeason.ToString(),
                Club = club.UniqueIndex.ToString(),
                WithDetails = true,
                WithDetailsSpecified = true,
            }));

            _logger.Information($"Syncing #{matchesResponse.GetMatchesResponse.MatchCount} Matches for {club.Name} ({club.CategoryName})");
            var matches = matchesResponse.GetMatchesResponse.TeamMatchesEntries ?? [];
            foreach (var match in matches)
            {
                await SyncMatch(match, matchUniqueIds);
            }

            if (matches.All(x => x.Date.AddHours(7) < DateTime.Now))
            {
                club.SyncCompleted = true;
            }

            await _db.SaveChangesAsync();
            _logger.Information("Synced Matches for Club");
        }
    }

    private async Task SyncMatch(TeamMatchEntryType match, List<int> matchUniqueIds)
    {
        if (!match.IsValidated)
            return;

        if (match.IsAwayForfeited || match.IsHomeForfeited)
            return;

        if (match.Score == null)
            return;

        if (matchUniqueIds.Any(id => id == int.Parse(match.MatchUniqueId)))
            return;

        foreach (var game in match.MatchDetails.IndividualMatchResults)
        {
            if (game.IsAwayForfeited || game.IsHomeForfeited)
                continue;

            if (game.AwayPlayerUniqueIndex is not { Length: 1 } || game.HomePlayerUniqueIndex is not { Length: 1 })
                continue;

            if (game.AwaySetCount == null || game.HomeSetCount == null)
                continue;

            var unspecifiedDate = match.Date.Add(match.Time.TimeOfDay);
            var date = new DateTimeOffset(unspecifiedDate, BelgianTimeZone.GetUtcOffset(unspecifiedDate));

            var matchEntity = new MatchEntity()
            {
                Competition = _settings.Competition,
                Year = _settings.Year,
                Date = date.UtcDateTime,
                WeekName = match.WeekName,
                MatchId = match.MatchId,
                MatchUniqueId = int.Parse(match.MatchUniqueId),
                Away = new MatchEntityPlayer()
                {
                    PlayerUniqueIndex = int.Parse(game.AwayPlayerUniqueIndex.Single()),
                    SetCount = int.Parse(game.AwaySetCount),
                },
                Home = new MatchEntityPlayer()
                {
                    PlayerUniqueIndex = int.Parse(game.HomePlayerUniqueIndex.Single()),
                    SetCount = int.Parse(game.HomeSetCount),
                }
            };
            await _db.Matches.AddAsync(matchEntity);
            matchUniqueIds.Add(matchEntity.MatchUniqueId);
        }
    }

    private async Task<List<ClubEntity>> SyncClubs()
    {
        var clubEntities = await _db.Clubs
            .Where(x => x.Competition == _settings.Competition && x.Year == _settings.Year)
            .ToListAsync();

        if (clubEntities.Count > 0)
            return clubEntities;

        var clubs = await _frenoy.GetClubsAsync(new GetClubsRequest(new GetClubs()
        {
            Season = _settings.FrenoySeason.ToString()
        }));

        _logger.Information($"Syncing Clubs (#{clubs.GetClubsResponse.ClubCount})");
        foreach (var club in clubs.GetClubsResponse.ClubEntries)
        {
            var clubEntity = new ClubEntity()
            {
                Name = club.Name,
                Competition = _settings.Competition,
                Year = _settings.Year,
                UniqueIndex = club.UniqueIndex,
                Category = int.Parse(club.Category),
                CategoryName = club.CategoryName,
            };
            await _db.Clubs.AddAsync(clubEntity);
            clubEntities.Add(clubEntity);
        }

        await _db.SaveChangesAsync();
        _logger.Information("Synced Clubs");

        return clubEntities;
    }

    private async Task SyncPlayers(ICollection<ClubEntity> clubs)
    {
        var playerEntities = await _db.Players
            .Where(x => x.Competition == _settings.Competition && x.Year == _settings.Year)
            .ToListAsync();

        if (playerEntities.Count > 0)
            return;

        var members = await _frenoy.GetMembersAsync(new GetMembersRequest1(new GetMembersRequest()
        {
            Season = _settings.FrenoySeason.ToString(),
            NameSearch = "",
        }));

        _logger.Information($"Syncing Players (#{members.GetMembersResponse.MemberCount})");
        foreach (var member in members.GetMembersResponse.MemberEntries)
        {
            var club = clubs.FirstOrDefault(x => x.UniqueIndex == member.Club);
            if (club == null)
            {
                continue;
            }
            var player = new PlayerEntity()
            {
                Competition = _settings.Competition,
                Year = _settings.Year,
                CategoryName = club?.CategoryName ?? "",
                UniqueIndex = int.Parse(member.UniqueIndex),
                FirstName = member.FirstName,
                LastName = member.LastName,
                Club = member.Club,
                ClubName = club?.Name ?? "",
                Ranking = member.Ranking,
            };
            await _db.Players.AddAsync(player);
        }
        await _db.SaveChangesAsync();
        _logger.Information("Synced Players");
    }
}
