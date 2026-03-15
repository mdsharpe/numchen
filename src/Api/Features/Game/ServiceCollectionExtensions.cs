namespace Numchen.Api.Features.Game;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameFeature(this IServiceCollection services)
    {
        services.AddSingleton<GameSessionStore>();
        return services;
    }

    public static WebApplication MapGameFeature(this WebApplication app)
    {
        app.MapHub<GameHub>("/game");
        return app;
    }
}
