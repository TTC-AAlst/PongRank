using Microsoft.AspNetCore.Mvc;
using PongRank.FrenoyApi;

namespace PongRank.WebApi.Controllers;

[Route("api/[controller]")]
public class FrenoySyncController
{
    private readonly FrenoyApiClient _frenoy;

    public FrenoySyncController(FrenoyApiClient frenoy)
    {
        _frenoy = frenoy;
    }

    /// <summary>
    /// Step 1: Sync Clubs, Players, Matches and Tournaments from Frenoy
    /// </summary>
    [HttpPost(nameof(Sync))]
    public async Task Sync(FrenoySettings settings)
    {
        _frenoy.Open(settings);
        await _frenoy.Sync();
    }
}
