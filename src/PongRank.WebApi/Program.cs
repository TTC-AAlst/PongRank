using Itenium.Forge.Controllers;
using Itenium.Forge.HealthChecks;
using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
using Itenium.Forge.Swagger;
using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.ML;
using PongRank.WebApi.Utilities;
using Serilog;
using Serilog.Context;

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.AddForgeSettings<WebApiSettings>();
    builder.Services.AddSingleton(settings.ML);
    builder.Services.AddSingleton(settings.SyncJob);
    builder.Services.AddSingleton(settings.CurrentYear);

    builder.AddForgeLogging();
    builder.AddForgeControllers();
    builder.AddForgeHealthChecks();

    builder.AddForgeSwagger(typeof(TrainingService), typeof(FrenoySettings));

    GlobalBackendConfiguration.Configure(builder.Services, builder.Configuration);
    builder.Services.AddScoped<PredictionService>();
    builder.Services.AddScoped<FrenoyApiClient>();
    builder.Services.AddScoped<TrainingService>();
    if (settings.StartSyncJob)
    {
        builder.Services.AddHostedService<FrenoySyncJob>();
        builder.Services.AddHostedService<CurrentSeasonSyncJob>();
    }

    var app = builder.Build();
    app.UseForgeLogging();
    app.UseForgeHealthChecks();

    app.UseForgeSwagger();

    app.Use(async (context, next) =>
    {
        LogContext.PushProperty("UserName", context.User.Identity?.Name ?? "Anonymous");
        await next();
    });

    app.UseForgeControllers();
    app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

    GlobalBackendConfiguration.MigrateDb(app.Services);

    app.Run();
}
catch (Exception ex)
{
    Log.Error(ex, "Something went wrong");
}
finally
{
    await Log.CloseAndFlushAsync();
}
