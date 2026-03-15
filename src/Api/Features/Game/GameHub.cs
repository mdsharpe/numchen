using Microsoft.AspNetCore.SignalR;

namespace Numchen.Api.Features.Game;

public class GameHub : Hub
{
    private readonly GameSessionStore _store;

    public GameHub(GameSessionStore store)
    {
        _store = store;
    }

    public async Task<string> CreateGame(string playerName)
    {
        var session = _store.CreateSession();

        session.JoinPlayer(Context.ConnectionId, playerName);

        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        await Clients.Caller.SendAsync("PlayerJoined", playerName);

        return session.JoinCode;
    }

    public async Task JoinGame(string joinCode, string playerName)
    {
        var session = _store.GetSessionByJoinCode(joinCode)
            ?? throw new HubException("Game not found.");

        session.JoinPlayer(Context.ConnectionId, playerName);

        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        await Clients.Group(session.Id).SendAsync("PlayerJoined", playerName);
    }
}
