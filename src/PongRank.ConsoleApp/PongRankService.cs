using Microsoft.Extensions.Hosting;
using PongRank.ConsoleApp.Utilities;
using PongRank.DataEntities.Core;
using PongRank.FrenoyApi;
using PongRank.Model.Core;
using Serilog;

namespace PongRank.ConsoleApp;

public class PongRankService : IHostedService
{
    private readonly ConsoleSettings _settings;
    private readonly ITtcDbContext _db;
    private readonly FrenoyApiClient _frenoyClient;
    private readonly TtcLogger _logger;

    public PongRankService(
        ConsoleSettings settings,
        ITtcDbContext db,
        FrenoyApiClient frenoyClient,
        TtcLogger logger)
    {
        _settings = settings;
        _db = db;
        _frenoyClient = frenoyClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var competition in _settings.Competitions)
        {
            foreach (int year in _settings.Seasons)
            {
                var frenoySettings = new FrenoySettings(competition, year, _settings.CategoryNames);
                _logger.Information($"Sync for {competition} {frenoySettings.Year} ({frenoySettings.FrenoySeason})");
                _frenoyClient.Open(frenoySettings);
                await _frenoyClient.Sync();
                _logger.Information("Sync completed");
            }
        }
        _logger.Information("PongRank Sync DONE");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}