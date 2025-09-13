using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.Model;

namespace PongRank.WebApi.Utilities;

/// <summary>
/// Syncing the current year matches for this year predictions
/// </summary>
public class CurrentSeasonSyncJob : IHostedService, IDisposable
{
    private readonly IServiceProvider _services;
    private Timer? _timer;

    private static int CurrentYear => DateTime.Now.Month < 9 ? DateTime.Now.Year - 1 : DateTime.Now.Year;

    public CurrentSeasonSyncJob(IServiceProvider services)
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
        var jobSettings = scope.ServiceProvider.GetRequiredService<CurrentYearSettings>();
        try
        {
            logger.LogInformation("CurrentSeasonSyncJob Started at {SyncStart}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            await SyncMatches(scope, Competition.Vttl, jobSettings.Vttl);
            await SyncMatches(scope, Competition.Sporta, jobSettings.Sporta);

            var aggregateService = scope.ServiceProvider.GetRequiredService<AggregateService>();
            await aggregateService.CalculateAndSave(Competition.Vttl, CurrentYear, jobSettings.Vttl);
            await aggregateService.CalculateAndSave(Competition.Sporta, CurrentYear, jobSettings.Sporta);
            
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)DateTime.Now.DayOfWeek + 7);
            _timer?.Change(TimeSpan.FromDays(daysUntilSunday), Timeout.InfiniteTimeSpan);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CurrentSeasonSyncJob failed {ErrorMessage}", ex.Message);
            _timer?.Change(TimeSpan.FromHours(5), Timeout.InfiniteTimeSpan);
        }
    }

    private static async Task SyncMatches(IServiceScope scope, Competition competition, string clubUniqueIndex)
    {
        var settings = new FrenoySettings(competition, CurrentYear, []);

        var frenoy = scope.ServiceProvider.GetRequiredService<FrenoyApiClient>();
        frenoy.Open(settings);
        await frenoy.SyncMatches(clubUniqueIndex);
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