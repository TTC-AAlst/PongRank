using Microsoft.AspNetCore.Mvc;
using PongRank.DataEntities;
using PongRank.ML;
using PongRank.Model;

namespace PongRank.WebApi.Controllers;

[Route("api/[controller]")]
public class TrainingController
{
    private readonly TrainingService _service;
    
    public TrainingController(TrainingService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create models for Vttl and Sporta competitions for
    /// existing <see cref="PlayerResultsEntity"/> records
    /// </summary>
    [HttpPost]
    public async Task Train()
    {
        await _service.Train(Competition.Sporta);
        await _service.Train(Competition.Vttl);
    }
}