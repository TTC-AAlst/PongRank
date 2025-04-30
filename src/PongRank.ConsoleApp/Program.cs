using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PongRank.ConsoleApp;
using PongRank.ConsoleApp.Utilities;
using PongRank.DataAccess;
using PongRank.DataEntities.Core;
using PongRank.FrenoyApi;
using PongRank.ML;
using PongRank.Model.Startup;
using Serilog;

Console.WriteLine("PongRank Startup");

SetupLogger.Configure("console.txt");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var (settings, configuration) = LoadSettings.Configure<ConsoleSettings>(services);
        services.AddScoped<FrenoyApiClient>();
        services.AddScoped<AggregateService>();
        services.AddScoped<TrainingService>();
        GlobalBackendConfiguration.Configure(services, configuration);
        services.AddHostedService<PongRankService>();
    })
    .Build();

try
{
    GlobalBackendConfiguration.MigrateDb(host.Services);
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "Something went wrong");
}
finally
{
    await Log.CloseAndFlushAsync();
}
