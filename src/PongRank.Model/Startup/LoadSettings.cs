using Microsoft.Extensions.Configuration;

namespace PongRank.Model.Startup;

public static class LoadSettings
{
    public static (T, IConfigurationRoot) GetConfiguration<T>() where T : class, new()
    {
        var settings = new T();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
            .Build();

        configuration
            .GetSection(typeof(T).Name)
            .Bind(settings);

        return (settings, configuration);
    }
}