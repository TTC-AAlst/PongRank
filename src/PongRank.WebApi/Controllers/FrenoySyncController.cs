using Microsoft.AspNetCore.Mvc;
using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.Model;

namespace PongRank.WebApi.Controllers;

[Route("api/[controller]")]
public class FrenoySyncController
{
    private readonly FrenoyApiClient _frenoy;
    private readonly AggregateService _aggregateService;

    public FrenoySyncController(FrenoyApiClient frenoy, AggregateService aggregateService)
    {
        _frenoy = frenoy;
        _aggregateService = aggregateService;
    }

    /// <summary>
    /// Sync Clubs, Players, Matches and Tournaments from Frenoy
    /// </summary>
    [HttpPost(nameof(Sync))]
    public async Task Sync(FrenoySettings settings)
    {
        _frenoy.Open(settings);
        await _frenoy.Sync();
    }

    /// <summary>
    /// Aggregate the synced match results from Frenoy for ML training
    /// </summary>
    /// <param name="competition"></param>
    /// <param name="year">The actual year (not Frenoy season)</param>
    [HttpPost(nameof(Aggregate))]
    public async Task Aggregate(Competition competition, int year)
    {
        await _aggregateService.CalculateAndSave(competition, year);
    }
}