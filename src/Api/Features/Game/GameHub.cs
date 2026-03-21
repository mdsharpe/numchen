using Microsoft.AspNetCore.SignalR;

namespace Numchen.Api.Features.Game;

public class GameHub : Hub
{
    private readonly GameSessionStore _store;

    public GameHub(GameSessionStore store)
    {
        _store = store;
    }

    public async Task<object> CreateGame(string playerName)
    {
        var session = _store.CreateSession();

        session.JoinPlayer(Context.ConnectionId, playerName);

        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);

        return new { JoinCode = session.JoinCode, Players = session.GetPlayerNames() };
    }

    public async Task<object> JoinGame(string joinCode, string playerName)
    {
        var session = _store.GetSessionByJoinCode(joinCode)
            ?? throw new HubException("Game not found.");

        session.JoinPlayer(Context.ConnectionId, playerName);

        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        await Clients.OthersInGroup(session.Id).SendAsync("PlayerJoined", playerName);

        return new { Players = session.GetPlayerNames() };
    }

    public async Task StartGame()
    {
        var session = GetSessionForCurrentConnection();

        Domain.Card card;
        lock (session.Lock)
        {
            session.Game.Start();
            card = session.Game.DrawCard();
        }

        await Clients.Group(session.Id).SendAsync("CardDrawn", card.Value);
    }

    public async Task PlaceCard(int columnIndex)
    {
        var session = GetSessionForCurrentConnection();

        Domain.Card? nextCard = null;
        bool finished = false;

        lock (session.Lock)
        {
            session.Game.PlaceCard(Context.ConnectionId, columnIndex);

            if (session.Game.State == Domain.GameState.ReadyToDraw)
            {
                nextCard = session.Game.DrawCard();
            }
            else if (session.Game.State == Domain.GameState.Finished)
            {
                finished = true;
            }
        }

        if (nextCard is not null)
        {
            await Clients.Group(session.Id).SendAsync("CardDrawn", nextCard.Value.Value);
        }
        else if (finished)
        {
            await Clients.Group(session.Id).SendAsync("GameFinished");
        }
    }

    public object MoveToDestination(int columnIndex)
    {
        var session = GetSessionForCurrentConnection();

        lock (session.Lock)
        {
            var pileIndex = session.Game.MoveToDestination(Context.ConnectionId, columnIndex);
            return new { PileIndex = pileIndex };
        }
    }

    private GameSession GetSessionForCurrentConnection()
    {
        return _store.GetSessionByConnectionId(Context.ConnectionId)
            ?? throw new HubException("Not in a game.");
    }
}
