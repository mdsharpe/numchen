using System.Collections.Concurrent;

namespace Numchen.Api.Features.Game;

public class GameSessionStore
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly int _maxCardValue;

    public GameSessionStore(int maxCardValue = Domain.Card.MaxValue)
    {
        _maxCardValue = maxCardValue;
    }

    public GameSession CreateSession()
    {
        var joinCode = GenerateJoinCode();
        var session = new GameSession(joinCode, _maxCardValue);
        _sessions[session.Id] = session;
        return session;
    }

    public GameSession? GetSessionByJoinCode(string joinCode)
    {
        return _sessions.Values
            .FirstOrDefault(s => s.JoinCode.Equals(joinCode, StringComparison.OrdinalIgnoreCase));
    }

    public GameSession? GetSessionByConnectionId(string connectionId)
    {
        return _sessions.Values
            .FirstOrDefault(s => s.GetHasPlayer(connectionId));
    }

    public GameSession? GetSessionByPlayerId(string playerId)
    {
        return _sessions.Values
            .FirstOrDefault(s => s.GetHasPlayerId(playerId));
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Create(4, chars, (span, state) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = state[Random.Shared.Next(state.Length)];
            }
        });
    }
}
