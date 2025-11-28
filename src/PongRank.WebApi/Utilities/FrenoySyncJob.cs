using PongRank.FrenoyApi;

namespace PongRank.WebApi.Utilities;

/// <summary>
/// Syncing based on the <see cref="SyncJobSettings"/>. This is for building historical data.
/// </summary>
public class FrenoySyncJob : IHostedService, IDisposable
{
    private readonly IServiceProvider _services;
    private Timer? _timer;

    public FrenoySyncJob(IServiceProvider services)
    {
        _services = services;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(async _ => await SyncMatches(), null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        return Task.CompletedTask;
    }

    private async Task SyncMatches()
    {
        using var scope = _services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FrenoySyncJob>>();
        var jobSettings = scope.ServiceProvider.GetRequiredService<SyncJobSettings>();
        try
        {
            logger.LogInformation("SyncJob Started at {SyncStart}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            logger.LogInformation("SyncJob with {@Settings}", jobSettings);

            var frenoy = scope.ServiceProvider.GetRequiredService<FrenoyApiClient>();
            foreach (var competition in jobSettings.SyncCompetitions)
            {
                foreach (int year in jobSettings.SyncYears)
                {
                    logger.LogInformation("FrenoySync for {competition} {year}", competition, year);
                    var settings = new FrenoySettings(competition, year, jobSettings.SyncCategoryNames);
                    frenoy.Open(settings);
                    await frenoy.Sync();
                }
            }

            logger.LogInformation("SyncJob Ended at {SyncStart}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            _timer?.Change(TimeSpan.FromHours(12), Timeout.InfiniteTimeSpan);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Quota exceeded"))
            {
                logger.LogWarning("FrenoySyncJob failed {ErrorMessage}", ex.Message);
            }
            else
            {
                logger.LogError(ex, "FrenoySyncJob failed {ErrorMessage}", ex.Message);
            }
            _timer?.Change(TimeSpan.FromHours(12), Timeout.InfiniteTimeSpan);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
