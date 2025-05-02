using PongRank.FrenoyApi;
using PongRank.Model;
using PongRank.Model.Core;

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
        var logger = scope.ServiceProvider.GetRequiredService<TtcLogger>();
        try
        {
            logger.Information($"SyncJob Started at {DateTime.Now:dd/MM/yyyy}");

            var frenoy = scope.ServiceProvider.GetRequiredService<FrenoyApiClient>();

            Competition[] competitions = [Competition.Vttl];
            int[] years = [2023, 2024, 2022, 2021];
            foreach (var competition in competitions)
            {
                foreach (int year in years)
                {
                    logger.Information($"FrenoySync for {competition} {year}");
                    var settings = new FrenoySettings(competition, year, []);
                    frenoy.Open(settings);
                    await frenoy.Sync();
                }
            }

            _timer?.Change(TimeSpan.FromHours(6), Timeout.InfiniteTimeSpan);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "FrenoySyncJob failed {Message}", ex.Message);
            _timer?.Change(TimeSpan.FromHours(6), Timeout.InfiniteTimeSpan);
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
    }
}