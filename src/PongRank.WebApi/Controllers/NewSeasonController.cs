using Microsoft.AspNetCore.Mvc;
using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.Model;

namespace PongRank.WebApi.Controllers;

[Route("api/[controller]")]
public class NewSeasonController
{
    private readonly FrenoyApiClient _frenoy;
    private readonly AggregateService _aggregateService;

    public NewSeasonController(FrenoyApiClient frenoy, AggregateService aggregateService)
    {
        _frenoy = frenoy;
        _aggregateService = aggregateService;
    }

    /// <summary>
    /// Start Season 1: Sync Clubs and Players from Frenoy
    /// </summary>
    [HttpPost(nameof(StartSyncForNewSeason))]
    public async Task StartSyncForNewSeason()
    {
        var settings = new FrenoySettings(Competition.Vttl, DateTime.Now.Year, []);
        _frenoy.Open(settings);
        await _frenoy.StartSyncForNewSeason();

        settings.Competition = Competition.Sporta;
        _frenoy.Open(settings);
        await _frenoy.StartSyncForNewSeason();
    }

    /// <summary>
    /// Start Season 2: Update PlayerResults.NextRanking from previous year
    /// </summary>
    [HttpPost(nameof(SetRankingsPreviousYear))]
    public async Task SetRankingsPreviousYear()
    {
        await _aggregateService.SetRankingsPreviousYear();
    }
}
