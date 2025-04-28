namespace PongRank.WebApi.Utilities;

public static class AddSwagger
{
    public static void Configure(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            
        });
    }
}