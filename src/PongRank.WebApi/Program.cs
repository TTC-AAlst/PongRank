using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.ML;
using PongRank.WebApi.Utilities;
using Serilog;
using Serilog.Context;
using System.Text.Json.Serialization;

Log.Logger = LoggingExtensions.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.AddForgeSettings<WebApiSettings>();
    builder.Services.AddSingleton(settings.ML);
    builder.Services.AddSingleton(settings.SyncJob);
    builder.Services.AddSingleton(settings.CurrentYear);

    builder.AddForgeLogging();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", corsBuilder =>
        {
            corsBuilder
                .WithOrigins(settings.Origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
    
    builder.Services.AddControllers().AddControllersAsServices().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.WriteIndented = false;
    });
    builder.Services.AddEndpointsApiExplorer();
    AddSwagger.Configure(builder.Services);
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

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("CorsPolicy");

    app.Use(async (context, next) =>
    {
        LogContext.PushProperty("UserName", context.User.Identity?.Name ?? "Anonymous");
        await next();
    });

    app.MapControllers();
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
