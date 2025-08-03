using PongRank.DataAccess;
using PongRank.FrenoyApi;
using PongRank.ML;
using PongRank.Model.Startup;
using PongRank.WebApi.Utilities;
using Serilog;
using Serilog.Context;
using System.Text.Json.Serialization;

try
{
    var (settings, configuration) = LoadSettings.GetConfiguration<WebApiSettings>();
    SetupLogger.Configure("webapi.txt", settings.Loki);

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSingleton(settings);
    builder.Services.AddSingleton(settings.ML);
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
    GlobalBackendConfiguration.Configure(builder.Services, configuration);
    builder.Services.AddScoped<PredictionService>();
    builder.Services.AddScoped<FrenoyApiClient>();
    builder.Services.AddScoped<TrainingService>();
    if (settings.StartSyncJob)
    {
        builder.Services.AddHostedService<FrenoySyncJob>();
    }

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var app = builder.Build();
    if (app.Environment.IsDevelopment())
    {
        Log.Information("Starting Development Server");
        app.UseDeveloperExceptionPage();
    }
    else
    {
        Log.Information("Starting Release Server");
    }

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors("CorsPolicy");

    app.Use(async (context, next) =>
    {
        LogContext.PushProperty("UserName", context.User.Identity?.Name ?? "Anonymous");
        LogContext.PushProperty("env", app.Environment.EnvironmentName);
        await next();
    });

    app.UseMiddleware<RequestLoggingFilter>();

    app.MapControllers();
    app.UseExceptionHandler();
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
