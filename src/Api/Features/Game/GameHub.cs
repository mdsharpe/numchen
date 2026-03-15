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

    public async Task StartGame()
    {
        var session = GetSessionForCurrentConnection();

        session.Game.Start();
        var card = session.Game.DrawCard();

        await Clients.Group(session.Id).SendAsync("CardDrawn", card.Value);
    }

    public async Task PlaceCard(int columnIndex)
    {
        var session = GetSessionForCurrentConnection();

        session.Game.PlaceCard(Context.ConnectionId, columnIndex);

        if (session.Game.State == Domain.GameState.ReadyToDraw)
        {
            var card = session.Game.DrawCard();
            await Clients.Group(session.Id).SendAsync("CardDrawn", card.Value);
        }
        else if (session.Game.State == Domain.GameState.Finished)
        {
            await Clients.Group(session.Id).SendAsync("GameFinished");
        }
    }

    public void MoveToDestination(int columnIndex)
    {
        var session = GetSessionForCurrentConnection();
        session.Game.MoveToDestination(Context.ConnectionId, columnIndex);
    }

    private GameSession GetSessionForCurrentConnection()
    {
        return _store.GetSessionByConnectionId(Context.ConnectionId)
            ?? throw new HubException("Not in a game.");
    }
}
