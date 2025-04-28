using Microsoft.Extensions.Hosting;
using PongRank.ConsoleApp.Utilities;
using PongRank.DataEntities.Core;
using PongRank.FrenoyApi;
using PongRank.Model.Core;

namespace PongRank.ConsoleApp;

public class PongRankService : IHostedService
{
    private readonly ConsoleSettings _settings;
    private readonly ITtcDbContext _db;
    private readonly FrenoyApiClient _frenoyClient;
    private readonly TtcLogger _logger;
    private readonly AggregateService _aggregateService;

    public PongRankService(
        ConsoleSettings settings,
        ITtcDbContext db,
        FrenoyApiClient frenoyClient,
        TtcLogger logger,
        AggregateService aggregateService)
    {
        _settings = settings;
        _db = db;
        _frenoyClient = frenoyClient;
        _logger = logger;
        _aggregateService = aggregateService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var competition in _settings.Competitions)
        {
            foreach (int year in _settings.Seasons.OrderByDescending(x => x))
            {
                _logger.Information($"Start {competition} {year}");

                if (_settings.FrenoySync)
                {
                    var frenoySettings = new FrenoySettings(competition, year, _settings.CategoryNames);
                    _logger.Information($"FrenoySync for {competition} {frenoySettings.Year} ({frenoySettings.FrenoySeason})");
                    _frenoyClient.Open(frenoySettings);
                    await _frenoyClient.Sync();
                    _logger.Information("FrenoySync Completed");
                }

                if (_settings.AggregateResults)
                {
                    await _aggregateService.CalculateAndSave(competition, year);
                }
            }
        }
        _logger.Information("PongRank Sync DONE");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}