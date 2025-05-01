using Microsoft.AspNetCore.Mvc;
using PongRank.ML;
using PongRank.ML.Models;

namespace PongRank.WebApi.Controllers;

[Route("api/[controller]")]
public class PredictionController
{
    private readonly PredictionService _service;

    public PredictionController(PredictionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Predict new rankings for the entire club
    /// </summary>
    [HttpGet]
    public Task<IEnumerable<PredictionResult>> Get(PredictionRequest request)
    {
        var result = _service.Predict(request);

        //var toCompare = result.Result
        //    .OrderBy(x => x.Name)
        //    .ToArray();

        return result;
    }
}
