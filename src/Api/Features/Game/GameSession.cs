namespace Numchen.Api.Features.Game;

public class GameSession
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string JoinCode { get; }
    public Domain.Game Game { get; }
    public Lock Lock { get; } = new();
    private readonly Dictionary<string, string> _playerIdsByConnectionId = new();
    private readonly Dictionary<string, string> _connectionIdsByPlayerId = new();
    private readonly Dictionary<string, string> _playerNamesByPlayerId = new();
    private readonly Dictionary<string, CancellationTokenSource> _disconnectTimers = new();
    private CancellationTokenSource? _placementTimerCts;
    public DateTimeOffset? PlacementDeadline { get; private set; }

    public GameSession(string joinCode)
    {
        JoinCode = joinCode;
        Game = new Domain.Game();
    }

    public string JoinPlayer(string connectionId, string playerName)
    {
        var playerId = Guid.NewGuid().ToString();
        Game.AddPlayer(playerId);
        _playerIdsByConnectionId[connectionId] = playerId;
        _connectionIdsByPlayerId[playerId] = connectionId;
        _playerNamesByPlayerId[playerId] = playerName;
        return playerId;
    }

    public void RejoinPlayer(string connectionId, string playerId)
    {
        if (!_playerNamesByPlayerId.ContainsKey(playerId))
        {
            throw new InvalidOperationException("Player not found in this game.");
        }

        CancelDisconnectTimer(playerId);

        var oldConnectionId = _connectionIdsByPlayerId[playerId];
        _playerIdsByConnectionId.Remove(oldConnectionId);

        _playerIdsByConnectionId[connectionId] = playerId;
        _connectionIdsByPlayerId[playerId] = connectionId;
    }

    public bool GetHasPlayer(string connectionId)
    {
        return _playerIdsByConnectionId.ContainsKey(connectionId);
    }

    public bool GetHasPlayerId(string playerId)
    {
        return _playerNamesByPlayerId.ContainsKey(playerId);
    }

    public string GetPlayerId(string connectionId)
    {
        return _playerIdsByConnectionId[connectionId];
    }

    public string GetPlayerName(string connectionId)
    {
        var playerId = _playerIdsByConnectionId[connectionId];
        return _playerNamesByPlayerId[playerId];
    }

    public string GetPlayerNameByPlayerId(string playerId)
    {
        return _playerNamesByPlayerId[playerId];
    }

    public IReadOnlyList<string> GetPlayerNames()
    {
        return _playerNamesByPlayerId.Values.ToList();
    }

    public int GetPlayerScore(string playerId)
    {
        var board = Game.GetPlayerBoard(playerId);
        return Enumerable.Range(0, Domain.PlayerBoard.DestinationPileCount)
            .Sum(i => board.GetDestinationPileCardCount(i));
    }

    public bool GetHasPlayerPlaced(string playerId)
    {
        if (Game.State != Domain.GameState.PlacingCard)
        {
            return false;
        }

        return Game.ReadyPlayers.Contains(playerId);
    }

    public void StartDisconnectTimer(string playerId, Action<string, GameSession> onExpired, TimeSpan gracePeriod)
    {
        CancelDisconnectTimer(playerId);

        var cts = new CancellationTokenSource();
        _disconnectTimers[playerId] = cts;

        _ = Task.Delay(gracePeriod, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                onExpired(playerId, this);
            }
        }, TaskScheduler.Default);
    }

    public void CancelDisconnectTimer(string playerId)
    {
        if (_disconnectTimers.Remove(playerId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public string RemovePlayer(string playerId)
    {
        var playerName = _playerNamesByPlayerId[playerId];
        var connectionId = _connectionIdsByPlayerId[playerId];

        _playerIdsByConnectionId.Remove(connectionId);
        _connectionIdsByPlayerId.Remove(playerId);
        _playerNamesByPlayerId.Remove(playerId);
        _disconnectTimers.Remove(playerId);

        Game.RemovePlayer(playerId);

        return playerName;
    }

    public string? GetPlayerIdByConnectionId(string connectionId)
    {
        return _playerIdsByConnectionId.TryGetValue(connectionId, out var playerId) ? playerId : null;
    }

    public string GetConnectionIdByPlayerId(string playerId)
    {
        return _connectionIdsByPlayerId[playerId];
    }

    public void StartPlacementTimer(Action<GameSession> onExpired, TimeSpan timeout)
    {
        CancelPlacementTimer();

        PlacementDeadline = DateTimeOffset.UtcNow.Add(timeout);

        var cts = new CancellationTokenSource();
        _placementTimerCts = cts;

        _ = Task.Delay(timeout, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                onExpired(this);
            }
        }, TaskScheduler.Default);
    }

    public void CancelPlacementTimer()
    {
        PlacementDeadline = null;

        if (_placementTimerCts is not null)
        {
            _placementTimerCts.Cancel();
            _placementTimerCts.Dispose();
            _placementTimerCts = null;
        }
    }
}
