using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using System.Diagnostics;

namespace PongRank.Model.Startup;

public static class SetupLogger
{
    public static void Configure(string fileName, string lokiUrl)
    {
        Debug.Assert(fileName.EndsWith(".txt"), "Ex: webapi.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithMachineName()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}")
            .WriteTo.File(
                $"logs/{fileName}",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}",
                shared: true
            )
            .WriteTo.GrafanaLoki(
                lokiUrl,
                [
                    new LokiLabel() { Key = "service_name", Value = "ttc-ml" },
                    new LokiLabel() { Key = "app", Value = "ttc" },
                ],
                [
                    "level",
                    "MachineName",
                    "UserName",
                    "RequestId",
                    "app",
                    "env"
                ])
            .CreateLogger();

        Log.Information("Starting up...");
    }
}