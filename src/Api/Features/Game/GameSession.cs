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

    public IReadOnlyList<string> GetPlayerNames()
    {
        return _playerNamesByPlayerId.Values.ToList();
    }

    public bool GetHasPlayerPlaced(string playerId)
    {
        if (Game.State != Domain.GameState.PlacingCard)
        {
            return false;
        }

        return Game.ReadyPlayers.Contains(playerId);
    }
}
