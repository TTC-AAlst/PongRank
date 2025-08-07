using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PongRank.ConsoleApp;
using PongRank.ConsoleApp.Utilities;
using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.ML;
using Serilog;

Environment.CurrentDirectory = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            var settings = new ConsoleSettings();
            configuration.GetSection(nameof(ConsoleSettings)).Bind(settings);
            services.AddSingleton(settings);
            services.AddSingleton(settings.ML);

            services.AddSerilog((s, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext());

            GlobalBackendConfiguration.Configure(services, configuration);
            services.AddScoped<FrenoyApiClient>();
            services.AddScoped<TrainingService>();
            services.AddHostedService<PongRankService>();
        })
        .Build();

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
