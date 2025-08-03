using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PongRank.ConsoleApp;
using PongRank.ConsoleApp.Utilities;
using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.ML;
using PongRank.Model.Startup;
using Serilog;

Console.WriteLine("PongRank Startup");

SetupLogger.Configure("console.txt", "http://localhost:3100");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var (settings, configuration) = LoadSettings.GetConfiguration<ConsoleSettings>();
        services.AddSingleton(settings);
        services.AddSingleton(settings.ML);
        GlobalBackendConfiguration.Configure(services, configuration);
        services.AddScoped<FrenoyApiClient>();
        services.AddScoped<TrainingService>();
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
