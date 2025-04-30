using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PongRank.ML;

namespace PongRank.WebApi.Controllers;

[Authorize]
[Route("api/ranking")]
public class RankingController
{
    private readonly PredictionService _service;

    public RankingController(PredictionService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public Task<IEnumerable<PredictionResult>> Get(PredictionRequest request)
    {
        return _service.Predict(request);
    }
}
