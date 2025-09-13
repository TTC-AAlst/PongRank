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
    /// Use the models from Step 3 to
    /// predict new rankings for the entire club
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<PredictionResult>> Get(PredictionRequest request)
    {
        var result = await _service.Predict(request);

        //var toCompare = result
        //    .OrderBy(x => x.Name)
        //    .ToArray();

        return result;
    }
}
