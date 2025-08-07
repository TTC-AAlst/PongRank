using PongRank.FrenoyApi;
using PongRank.Model;

namespace PongRank.WebApi.Utilities;

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
            logger.LogInformation("SyncJob Started at {SyncStart}", DateTime.Now.ToString("dd/MM/yyyy"));

            var frenoy = scope.ServiceProvider.GetRequiredService<FrenoyApiClient>();

            Competition[] competitions = [Competition.Vttl];
            int[] years = [2024, 2022, 2021];
            foreach (var competition in competitions)
            {
                foreach (int year in years)
                {
                    logger.LogInformation("FrenoySync for {competition} {year}", competition, year);
                    var settings = new FrenoySettings(competition, year, []);
                    frenoy.Open(settings);
                    await frenoy.Sync();
                }
            }

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