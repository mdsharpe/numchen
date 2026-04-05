using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Numchen.Api.Features.Game;

namespace Numchen.Api.Tests.Fixtures;

public class NumchenWebApplicationFactory : WebApplicationFactory<Program>
{
    public GameHubOptions HubOptions { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var storeDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(GameSessionStore));
            if (storeDescriptor is not null)
            {
                services.Remove(storeDescriptor);
            }

            var optionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(GameHubOptions));
            if (optionsDescriptor is not null)
            {
                services.Remove(optionsDescriptor);
            }

            services.AddSingleton(new GameSessionStore(maxCardValue: 3));
            services.AddSingleton(HubOptions);
        });
    }

    public HubConnection CreateHubConnection()
    {
        var server = Server;
        return new HubConnectionBuilder()
            .WithUrl($"{server.BaseAddress}game", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            })
            .Build();
    }
}
