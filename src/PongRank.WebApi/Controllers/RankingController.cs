using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PongRank.Model;

namespace PongRank.WebApi.Controllers;

[Authorize]
[Route("api/ranking")]
public class RankingController
{
    //private readonly RankingService _service;

    //public RankingController(RankingService service)
    //{
    //    _service = service;
    //}

    [HttpGet]
    [AllowAnonymous]
    public Task<string> Get(Competition competition, int uniqueIndex, int season)
    {
        return Task.FromResult("E6");
    }
}
