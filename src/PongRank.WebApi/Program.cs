using Serilog;
using System.Text.Json.Serialization;
using PongRank.Model.Startup;
using PongRank.WebApi.Utilities;
using PongRank.DataAccess;

SetupLogger.Configure("webapi.txt");

try
{
    var builder = WebApplication.CreateBuilder(args);
    var (settings, configuration) = LoadSettings.Configure<WebApiSettings>(builder.Services);
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
    
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    //builder.Services.AddServiceModelServices().AddServiceModelMetadata();
    //builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

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

    //var serviceMetadataBehavior = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
    //serviceMetadataBehavior.HttpGetEnabled = true;

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
