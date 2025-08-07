using Microsoft.AspNetCore.Mvc;

namespace PongRank.WebApi.Controllers;

[Route("api/[controller]")]
public class ConfigController
{
    #region Constructor
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(ILogger<ConfigController> logger)
    {
        _logger = logger;
    }
    #endregion

    [HttpGet("Logging")]
    public string GetLogging()
    {
        string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        _logger.LogInformation("Looking for last log dir in: {logDir}", logDir);
        string fileName = Directory
            .GetFiles(logDir, "*.txt")
            .OrderByDescending(x => x)
            .First();

        _logger.LogInformation("Current log file: {fileName}", fileName);
        return File.ReadAllText(fileName);
    }
}
