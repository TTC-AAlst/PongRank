using System.Diagnostics;
using Serilog;
using Serilog.Events;

namespace PongRank.Model.Startup;

public static class SetupLogger
{
    public static void Configure(string fileName)
    {
        Debug.Assert(fileName.EndsWith(".txt"), "Ex: webapi.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}")
            .WriteTo.File(
                $"logs/{fileName}",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}",
                shared: true
            )
            .CreateLogger();

        Log.Information("Starting up...");
    }
}