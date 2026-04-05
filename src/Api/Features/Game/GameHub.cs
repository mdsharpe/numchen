using Microsoft.AspNetCore.SignalR;

namespace Numchen.Api.Features.Game;

public class GameHub : Hub
{
    private static readonly TimeSpan DisconnectGracePeriod = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PlacementTimeout = TimeSpan.FromSeconds(45);

    private readonly GameSessionStore _store;
    private readonly IHubContext<GameHub> _hubContext;

    public GameHub(GameSessionStore store, IHubContext<GameHub> hubContext)
    {
        _store = store;
        _hubContext = hubContext;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var session = _store.GetSessionByConnectionId(Context.ConnectionId);
        var playerId = session?.GetPlayerIdByConnectionId(Context.ConnectionId);

        if (session is not null && playerId is not null)
        {
            lock (session.Lock)
            {
                session.StartDisconnectTimer(playerId, OnDisconnectTimerExpired, DisconnectGracePeriod);
            }
        }

        return base.OnDisconnectedAsync(exception);
    }

    private void OnDisconnectTimerExpired(string playerId, GameSession session)
    {
        string playerName;

        lock (session.Lock)
        {
            if (!session.GetHasPlayerId(playerId))
            {
                return;
            }

            playerName = session.RemovePlayer(playerId);
        }

        var clients = _hubContext.Clients.Group(session.Id);
        clients.SendAsync("PlayerLeft", playerId, playerName).GetAwaiter().GetResult();

        BroadcastGameStateAdvance(session, clients).GetAwaiter().GetResult();
    }

    private void OnPlacementTimerExpired(GameSession session)
    {
        IReadOnlyList<(string PlayerId, int ColumnIndex)> placements;

        lock (session.Lock)
        {
            if (session.Game.State != Domain.GameState.PlacingCard)
            {
                return;
            }

            placements = session.Game.AutoPlaceForUnreadyPlayers();
        }

        foreach (var (playerId, columnIndex) in placements)
        {
            var connectionId = session.GetConnectionIdByPlayerId(playerId);
            _hubContext.Clients.Client(connectionId)
                .SendAsync("CardAutoPlaced", columnIndex)
                .GetAwaiter().GetResult();

            var playerName = session.GetPlayerNameByPlayerId(playerId);
            _hubContext.Clients.Group(session.Id)
                .SendAsync("PlayerPlaced", playerId, playerName)
                .GetAwaiter().GetResult();
        }

        var clients = _hubContext.Clients.Group(session.Id);
        BroadcastGameStateAdvance(session, clients).GetAwaiter().GetResult();
    }

    public async Task<object> CreateGame(string playerName)
    {
        var session = _store.CreateSession();

        var playerId = session.JoinPlayer(Context.ConnectionId, playerName);

        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);

        return new
        {
            JoinCode = session.JoinCode,
            PlayerId = playerId,
            Players = session.Game.PlayerIds.Select(pid => new
            {
                Id = pid,
                Name = session.GetPlayerNameByPlayerId(pid),
            }).ToList()
        };
    }

    public async Task<object> JoinGame(string joinCode, string playerName)
    {
        var session = _store.GetSessionByJoinCode(joinCode)
            ?? throw new HubException("Game not found.");

        var playerId = session.JoinPlayer(Context.ConnectionId, playerName);

        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
        await Clients.OthersInGroup(session.Id).SendAsync("PlayerJoined", playerId, playerName);

        return new
        {
            PlayerId = playerId,
            Players = session.Game.PlayerIds.Select(pid => new
            {
                Id = pid,
                Name = session.GetPlayerNameByPlayerId(pid),
            }).ToList()
        };
    }

    public async Task<object> RejoinGame(string playerId)
    {
        var session = _store.GetSessionByPlayerId(playerId)
            ?? throw new HubException("Game not found.");

        lock (session.Lock)
        {
            session.RejoinPlayer(Context.ConnectionId, playerId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);

        var board = session.Game.GetPlayerBoard(playerId);
        var columns = new int[Domain.PlayerBoard.ColumnCount][];
        for (var i = 0; i < Domain.PlayerBoard.ColumnCount; i++)
        {
            columns[i] = board.GetColumnCards(i).Select(c => c.Value).ToArray();
        }

        var destinations = new int[Domain.PlayerBoard.DestinationPileCount];
        for (var i = 0; i < Domain.PlayerBoard.DestinationPileCount; i++)
        {
            destinations[i] = board.GetDestinationPileTopValue(i);
        }

        var players = session.Game.PlayerIds.Select(pid => new
        {
            Id = pid,
            Name = session.GetPlayerNameByPlayerId(pid),
            Score = session.GetPlayerScore(pid),
        }).ToList();

        var placedPlayers = session.Game.State == Domain.GameState.PlacingCard
            ? session.Game.ReadyPlayers.ToList()
            : new List<string>();

        return new
        {
            JoinCode = session.JoinCode,
            Players = players,
            PlacedPlayers = placedPlayers,
            GameStarted = session.Game.State != Domain.GameState.WaitingForPlayers,
            GameFinished = session.Game.State == Domain.GameState.Finished,
            CurrentCard = session.Game.CurrentCard?.Value,
            HasPlaced = session.GetHasPlayerPlaced(playerId),
            PlacementDeadline = session.PlacementDeadline?.ToUnixTimeMilliseconds(),
            Columns = columns,
            Destinations = destinations
        };
    }

    public async Task StartGame()
    {
        var session = GetSessionForCurrentConnection();

        Domain.Card card;
        lock (session.Lock)
        {
            session.Game.Start();
            card = session.Game.DrawCard();
            session.StartPlacementTimer(OnPlacementTimerExpired, PlacementTimeout);
        }

        var scores = GetAllScores(session);
        await Clients.Group(session.Id).SendAsync("CardDrawn", card.Value, session.PlacementDeadline?.ToUnixTimeMilliseconds(), scores);
    }

    public async Task PlaceCard(int columnIndex)
    {
        var session = GetSessionForCurrentConnection();
        var playerId = session.GetPlayerId(Context.ConnectionId);

        lock (session.Lock)
        {
            session.Game.PlaceCard(playerId, columnIndex);
        }

        var playerName = session.GetPlayerNameByPlayerId(playerId);
        await Clients.Group(session.Id).SendAsync("PlayerPlaced", playerId, playerName);
        await BroadcastGameStateAdvance(session, Clients.Group(session.Id));
    }

    public async Task<object> MoveToDestination(int columnIndex)
    {
        var session = GetSessionForCurrentConnection();
        var playerId = session.GetPlayerId(Context.ConnectionId);

        int pileIndex;
        int score;
        lock (session.Lock)
        {
            pileIndex = session.Game.MoveToDestination(playerId, columnIndex);
            score = session.GetPlayerScore(playerId);
        }

        var playerName = session.GetPlayerNameByPlayerId(playerId);
        await Clients.Group(session.Id).SendAsync("PlayerScored", playerId, playerName, score);

        return new { PileIndex = pileIndex };
    }

    private async Task BroadcastGameStateAdvance(GameSession session, IClientProxy clients)
    {
        Domain.Card? nextCard = null;
        bool finished = false;

        lock (session.Lock)
        {
            if (session.Game.State == Domain.GameState.ReadyToDraw)
            {
                nextCard = session.Game.DrawCard();
                session.StartPlacementTimer(OnPlacementTimerExpired, PlacementTimeout);
            }
            else if (session.Game.State == Domain.GameState.Finished)
            {
                session.CancelPlacementTimer();
                finished = true;
            }
        }

        if (nextCard is not null)
        {
            var scores = GetAllScores(session);
            await clients.SendAsync("CardDrawn", nextCard.Value.Value, session.PlacementDeadline?.ToUnixTimeMilliseconds(), scores);
        }
        else if (finished)
        {
            await clients.SendAsync("GameFinished");
        }
    }

    private static Dictionary<string, int> GetAllScores(GameSession session)
    {
        var scores = new Dictionary<string, int>();
        foreach (var pid in session.Game.PlayerIds)
        {
            scores[pid] = session.GetPlayerScore(pid);
        }
        return scores;
    }

    private GameSession GetSessionForCurrentConnection()
    {
        return _store.GetSessionByConnectionId(Context.ConnectionId)
            ?? throw new HubException("Not in a game.");
    }
}
