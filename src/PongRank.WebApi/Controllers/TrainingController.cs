using Microsoft.AspNetCore.Mvc;
using PongRank.DataAccess;
using PongRank.DataEntities;
using PongRank.ML;
using PongRank.Model;

namespace PongRank.WebApi.Controllers;

[Route("api/[controller]")]
public class TrainingController
{
    private readonly TrainingService _service;
    private readonly AggregateService _aggregateService;

    public TrainingController(TrainingService service, AggregateService aggregateService)
    {
        _service = service;
        _aggregateService = aggregateService;
    }

    /// <summary>
    /// Step 2: Aggregate the synced match results from Frenoy for ML training
    /// </summary>
    /// <param name="competition">Vttl or Sporta</param>
    /// <param name="year">The actual year (not Frenoy season)</param>
    [HttpPost(nameof(Aggregate))]
    public async Task Aggregate(Competition competition, int year)
    {
        await _aggregateService.CalculateAndSave(competition, year);
    }

    /// <summary>
    /// Step 2: Aggregate the synced match results from Frenoy
    /// for ML training for all years that are completely synced
    /// </summary>
    [HttpPost(nameof(AggregateAll))]
    public async Task AggregateAll(Competition competition)
    {
        await _aggregateService.CalculateAndSave(competition);
    }

    /// <summary>
    /// Step 3: Create models for Vttl and Sporta competitions
    /// for existing <see cref="PlayerResultsEntity"/> records
    ///
    /// PlayerResults are filled by the <see cref="AggregateService"/>.
    /// 
    /// For retraining, the actions from the <see cref="NewSeasonController"/>
    /// also must have happened (for the PlayerResults.NextRanking)
    /// </summary>
    [HttpPost]
    public async Task Train()
    {
        await _service.Train(Competition.Sporta);
        await _service.Train(Competition.Vttl);
    }
}