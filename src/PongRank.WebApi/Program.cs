using Serilog;
using System.Text.Json.Serialization;
using PongRank.Model.Startup;
using PongRank.WebApi.Utilities;
using PongRank.DataAccess;
using PongRank.ML;
using System.Web.Services.Description;
using PongRank.FrenoyApi;
using System.Runtime;

SetupLogger.Configure("webapi.txt");

try
{
    var builder = WebApplication.CreateBuilder(args);
    var (settings, configuration) = LoadSettings.Configure<WebApiSettings>(builder.Services);
    builder.Services.AddSingleton(settings.ML);
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", corsBuilder =>
        {
            corsBuilder
                .WithOrigins(settings.Origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
    
    builder.Services.AddControllers().AddControllersAsServices().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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
        Log.Information("Starting Development Server with Swagger");
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseDeveloperExceptionPage();
    }
    else
    {
        Log.Information("Starting Release Server");
    }

    app.UseCors("CorsPolicy");

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
