using Microsoft.AspNetCore.Http.Connections;

namespace Numchen.Api.Features.Game;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameFeature(this IServiceCollection services, IHostEnvironment environment)
    {
        var maxCardValue = environment.IsDevelopment() ? 3 : Domain.Card.MaxValue;
        services.AddSingleton(new GameSessionStore(maxCardValue));
        services.AddSingleton(new GameHubOptions());
        return services;
    }

    public static WebApplication MapGameFeature(this WebApplication app)
    {
        app.MapHub<GameHub>("/hub/game", options =>
        {
            options.Transports = HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
        });
        return app;
    }
}
