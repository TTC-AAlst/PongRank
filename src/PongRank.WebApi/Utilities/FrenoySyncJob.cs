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
        try
        {
            logger.LogInformation("SyncJob Started at {SyncStart}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            var runner = scope.ServiceProvider.GetRequiredService<HistoricalSyncRunner>();
            await runner.RunAsync();
            logger.LogInformation("SyncJob Ended at {SyncEnd}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FrenoySyncJob failed {ErrorMessage}", ex.Message);
        }
        finally
        {
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
