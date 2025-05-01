using Microsoft.Extensions.Hosting;
using PongRank.ConsoleApp.Utilities;
using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.ML;
using PongRank.Model.Core;

namespace PongRank.ConsoleApp;

public class PongRankService : IHostedService
{
    private readonly ConsoleSettings _settings;
    private readonly FrenoyApiClient _frenoyClient;
    private readonly TtcLogger _logger;
    private readonly AggregateService _aggregateService;
    private readonly TrainingService _trainingService;
    private readonly IHostApplicationLifetime _lifetime;

    public PongRankService(
        ConsoleSettings settings,
        FrenoyApiClient frenoyClient,
        TtcLogger logger,
        AggregateService aggregateService,
        TrainingService trainingService,
        IHostApplicationLifetime lifetime)
    {
        _settings = settings;
        _frenoyClient = frenoyClient;
        _logger = logger;
        _aggregateService = aggregateService;
        _trainingService = trainingService;
        _lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await StartAsync();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unexpected exception");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private async Task StartAsync()
    {
        foreach (var competition in _settings.Competitions)
        {
            foreach (int year in _settings.Seasons)
            {
                _logger.Information($"Start {competition} {year}");

                if (_settings.FrenoySync)
                {
                    var frenoySettings = new FrenoySettings(competition, year, _settings.CategoryNames);
                    _logger.Information($"FrenoySync for {competition} {frenoySettings.Year} ({frenoySettings.FrenoySeason})");
                    _frenoyClient.Open(frenoySettings);
                    await _frenoyClient.Sync();
                    _logger.Information($"FrenoySync {competition} {frenoySettings.Year}: Completed");
                }

                if (_settings.AggregateResults)
                {
                    _logger.Information("Aggregating Frenoy Results for ML");
                    if (_settings.CategoryNames.Length > 0)
                        _logger.Information("Settings.CategoryNames is set. This is ignored when aggregating results.");
                    await _aggregateService.CalculateAndSave(competition, year);
                    _logger.Information("Aggregating Frenoy Results for ML: DONE");
                }
            }

            if (_settings.Train)
            {
                _logger.Information($"Start Training ML for {competition}");
                await _trainingService.Train(competition);
                _logger.Information("Start Training ML: DONE");
            }
        }
        _logger.Information("PongRank Sync DONE");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}