namespace PongRank.WebApi.Utilities;

public static class AddSwagger
{
    public static void Configure(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "PongRank.WebApi.xml");
            c.IncludeXmlComments(filePath);

            var mlFilePath = Path.Combine(AppContext.BaseDirectory, "PongRank.ML.xml");
            c.IncludeXmlComments(mlFilePath);

            var apiFilePath = Path.Combine(AppContext.BaseDirectory, "PongRank.FrenoyApi.xml");
            c.IncludeXmlComments(apiFilePath);
        });
    }
}