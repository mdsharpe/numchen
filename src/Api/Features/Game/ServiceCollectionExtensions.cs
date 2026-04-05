namespace Numchen.Api.Features.Game;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameFeature(this IServiceCollection services, IHostEnvironment environment)
    {
        var maxCardValue = environment.IsDevelopment() ? 3 : Domain.Card.MaxValue;
        services.AddSingleton(new GameSessionStore(maxCardValue));
        return services;
    }

    public static WebApplication MapGameFeature(this WebApplication app)
    {
        app.MapHub<GameHub>("/game");
        return app;
    }
}
