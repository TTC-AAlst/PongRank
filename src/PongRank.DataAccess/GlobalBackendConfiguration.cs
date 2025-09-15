using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PongRank.DataEntities.Core;

namespace PongRank.DataAccess;

public static class GlobalBackendConfiguration
{
    public static void Configure(IServiceCollection services, IConfigurationRoot configuration)
    {
        ConfigureDbContext(services, configuration);
        services.AddScoped<AggregateService>();
    }

    private static void ConfigureDbContext(IServiceCollection services, IConfigurationRoot configuration)
    {
        var connectionString = configuration.GetConnectionString("Ttc");
        services.AddDbContext<ITtcDbContext, TtcDbContext>(
            dbContextOptions => ConfigureDbContextBuilder(dbContextOptions, connectionString));
    }

    internal static void ConfigureDbContextBuilder(DbContextOptionsBuilder builder, string? connectionString = null)
    {
        Serilog.Log.Logger.Information($"ConfigureDbContextBuilder with {connectionString}");
        if (connectionString == null)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../PongRank.WebApi"))
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .Build();
            connectionString = configuration.GetConnectionString("Ttc") ?? "";
            Serilog.Log.Logger.Information($"ConfigureDbContextBuilder setting from {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")} to {connectionString}");
        }

        string? dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (!string.IsNullOrWhiteSpace(dbPassword))
        {
            connectionString = connectionString.Replace("{DB_PASSWORD}", dbPassword);
        }

        Serilog.Log.Logger.Information($"ConfigureDbContextBuilder final {connectionString}");
        builder.UseNpgsql(connectionString);
    }

    public static void MigrateDb(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITtcDbContext>();
        dbContext.Database.Migrate();
    }
}
