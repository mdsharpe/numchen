namespace Numchen.Api.Features.Game;

public class GameSession
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string JoinCode { get; }
    public Domain.Game Game { get; }
    private readonly Dictionary<string, string> _playerNamesByConnectionId = new();

    public GameSession(string joinCode)
    {
        JoinCode = joinCode;
        Game = new Domain.Game();
    }

    public void JoinPlayer(string connectionId, string playerName)
    {
        Game.AddPlayer(connectionId);
        _playerNamesByConnectionId[connectionId] = playerName;
    }

    public bool GetHasPlayer(string connectionId)
    {
        return _playerNamesByConnectionId.ContainsKey(connectionId);
    }

    public string GetPlayerName(string connectionId)
    {
        return _playerNamesByConnectionId[connectionId];
    }
}
