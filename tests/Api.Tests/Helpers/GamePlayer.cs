using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;

namespace Numchen.Api.Tests.Helpers;

public class GamePlayer : IAsyncDisposable
{
    private readonly HubConnection _connection;

    public string? PlayerId { get; private set; }
    public string? JoinCode { get; private set; }
    public string PlayerName { get; }
    public int TotalCards { get; private set; }

    private readonly Channel<CardDrawnEvent> _cardDrawnChannel = Channel.CreateUnbounded<CardDrawnEvent>();
    private readonly Channel<PlayerPlacedEvent> _playerPlacedChannel = Channel.CreateUnbounded<PlayerPlacedEvent>();
    private readonly Channel<PlayerScoredEvent> _playerScoredChannel = Channel.CreateUnbounded<PlayerScoredEvent>();
    private readonly Channel<PlayerJoinedEvent> _playerJoinedChannel = Channel.CreateUnbounded<PlayerJoinedEvent>();
    private readonly Channel<PlayerLeftEvent> _playerLeftChannel = Channel.CreateUnbounded<PlayerLeftEvent>();
    private readonly Channel<int> _cardAutoPlacedChannel = Channel.CreateUnbounded<int>();
    private readonly Channel<CardDrawnEvent> _gameRestartedChannel = Channel.CreateUnbounded<CardDrawnEvent>();
    private readonly Channel<long?> _finishingPhaseStartedChannel = Channel.CreateUnbounded<long?>();
    private readonly Channel<bool> _gameFinishedChannel = Channel.CreateUnbounded<bool>();

    public GamePlayer(HubConnection connection, string playerName)
    {
        _connection = connection;
        PlayerName = playerName;

        _connection.On<int, long?, object>("CardDrawn", (value, deadline, scores) =>
        {
            _cardDrawnChannel.Writer.TryWrite(new CardDrawnEvent(value, deadline));
        });

        _connection.On<string, string>("PlayerPlaced", (playerId, playerName) =>
        {
            _playerPlacedChannel.Writer.TryWrite(new PlayerPlacedEvent(playerId, playerName));
        });

        _connection.On<string, string, int>("PlayerScored", (playerId, playerName, score) =>
        {
            _playerScoredChannel.Writer.TryWrite(new PlayerScoredEvent(playerId, playerName, score));
        });

        _connection.On<string, string>("PlayerJoined", (playerId, playerName) =>
        {
            _playerJoinedChannel.Writer.TryWrite(new PlayerJoinedEvent(playerId, playerName));
        });

        _connection.On<string, string>("PlayerLeft", (playerId, playerName) =>
        {
            _playerLeftChannel.Writer.TryWrite(new PlayerLeftEvent(playerId, playerName));
        });

        _connection.On<int>("CardAutoPlaced", (columnIndex) =>
        {
            _cardAutoPlacedChannel.Writer.TryWrite(columnIndex);
        });

        _connection.On<int, long?, object>("GameRestarted", (value, deadline, scores) =>
        {
            _gameRestartedChannel.Writer.TryWrite(new CardDrawnEvent(value, deadline));
        });

        _connection.On<long?, object>("FinishingPhaseStarted", (deadline, scores) =>
        {
            _finishingPhaseStartedChannel.Writer.TryWrite(deadline);
        });

        _connection.On("GameFinished", () =>
        {
            _gameFinishedChannel.Writer.TryWrite(true);
        });
    }

    public async Task ConnectAsync()
    {
        await _connection.StartAsync();
    }

    public async Task CreateGameAsync()
    {
        var result = await _connection.InvokeAsync<CreateGameResponse>("CreateGame", PlayerName);
        JoinCode = result.JoinCode;
        PlayerId = result.PlayerId;
        TotalCards = result.TotalCards;
    }

    public async Task JoinGameAsync(string joinCode)
    {
        var result = await _connection.InvokeAsync<JoinGameResponse>("JoinGame", joinCode, PlayerName);
        JoinCode = joinCode;
        PlayerId = result.PlayerId;
        TotalCards = result.TotalCards;
    }

    public async Task<RejoinGameResponse> RejoinGameAsync(string playerId)
    {
        var result = await _connection.InvokeAsync<RejoinGameResponse>("RejoinGame", playerId);
        PlayerId = playerId;
        JoinCode = result.JoinCode;
        TotalCards = result.TotalCards;
        return result;
    }

    public async Task StartGameAsync()
    {
        await _connection.InvokeAsync("StartGame");
    }

    public async Task RestartGameAsync()
    {
        await _connection.InvokeAsync("RestartGame");
    }

    public async Task PlaceCardAsync(int columnIndex)
    {
        await _connection.InvokeAsync("PlaceCard", columnIndex);
    }

    public async Task<MoveToDestinationResponse> MoveToDestinationAsync(int columnIndex)
    {
        return await _connection.InvokeAsync<MoveToDestinationResponse>("MoveToDestination", columnIndex);
    }

    public async Task<CardDrawnEvent> WaitForCardDrawnAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _cardDrawnChannel.Reader.ReadAsync(cts.Token);
    }

    public async Task<PlayerPlacedEvent> WaitForPlayerPlacedAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _playerPlacedChannel.Reader.ReadAsync(cts.Token);
    }

    public async Task<PlayerScoredEvent> WaitForPlayerScoredAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _playerScoredChannel.Reader.ReadAsync(cts.Token);
    }

    public async Task<PlayerJoinedEvent> WaitForPlayerJoinedAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _playerJoinedChannel.Reader.ReadAsync(cts.Token);
    }

    public async Task<PlayerLeftEvent> WaitForPlayerLeftAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _playerLeftChannel.Reader.ReadAsync(cts.Token);
    }

    public async Task<int> WaitForCardAutoPlacedAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _cardAutoPlacedChannel.Reader.ReadAsync(cts.Token);
    }

    public async Task<CardDrawnEvent> WaitForGameRestartedAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _gameRestartedChannel.Reader.ReadAsync(cts.Token);
    }

    public async Task<long?> WaitForFinishingPhaseStartedAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _finishingPhaseStartedChannel.Reader.ReadAsync(cts.Token);
    }

    public async Task WaitForGameFinishedAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        await _gameFinishedChannel.Reader.ReadAsync(cts.Token);
    }

    public bool TryReadCardDrawn(out CardDrawnEvent? result)
    {
        return _cardDrawnChannel.Reader.TryRead(out result);
    }

    public async Task StopAsync()
    {
        await _connection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

public record CardDrawnEvent(int Value, long? Deadline);
public record PlayerPlacedEvent(string PlayerId, string PlayerName);
public record PlayerScoredEvent(string PlayerId, string PlayerName, int Score);
public record PlayerJoinedEvent(string PlayerId, string PlayerName);
public record PlayerLeftEvent(string PlayerId, string PlayerName);

public class CreateGameResponse
{
    public string JoinCode { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public int TotalCards { get; set; }
    public List<PlayerInfo> Players { get; set; } = [];
}

public class JoinGameResponse
{
    public string PlayerId { get; set; } = "";
    public int TotalCards { get; set; }
    public List<PlayerInfo> Players { get; set; } = [];
}

public class RejoinGameResponse
{
    public string JoinCode { get; set; } = "";
    public int TotalCards { get; set; }
    public List<PlayerInfo> Players { get; set; } = [];
    public List<string> PlacedPlayers { get; set; } = [];
    public bool GameStarted { get; set; }
    public bool GameFinishing { get; set; }
    public bool GameFinished { get; set; }
    public int? CurrentCard { get; set; }
    public bool HasPlaced { get; set; }
    public long? PlacementDeadline { get; set; }
    public long? FinishingDeadline { get; set; }
    public int[][] Columns { get; set; } = [];
    public int[] Destinations { get; set; } = [];
}

public class MoveToDestinationResponse
{
    public int PileIndex { get; set; }
}

public class PlayerInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Score { get; set; }
}
