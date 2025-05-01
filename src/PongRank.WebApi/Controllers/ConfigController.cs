using Microsoft.AspNetCore.Mvc;
using PongRank.Model.Core;

namespace PongRank.WebApi.Controllers;

[Route("api/[controller]")]
public class ConfigController
{
    #region Constructor
    private readonly TtcLogger _logger;

    public ConfigController(TtcLogger logger)
    {
        _logger = logger;
    }
    #endregion

    [HttpGet("Logging")]
    public string GetLogging()
    {
        string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        _logger.Information($"Looking for last log dir in: {logDir}");
        string fileName = Directory
            .GetFiles(logDir, "*.txt")
            .OrderByDescending(x => x)
            .First();

        _logger.Information($"Current log file: {fileName}");
        return File.ReadAllText(fileName);
    }
}
