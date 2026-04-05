namespace Numchen.Api.Tests.Tests;

public class GameLifecycleTests : IAsyncDisposable
{
    private readonly NumchenWebApplicationFactory _factory = new();

    [Fact]
    public async Task CreateGame_ReturnsJoinCodeAndPlayerId()
    {
        await using var player = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");
        await player.CreateGameAsync();

        Assert.NotNull(player.JoinCode);
        Assert.Equal(4, player.JoinCode!.Length);
        Assert.NotNull(player.PlayerId);
        Assert.Equal(18, player.TotalCards);
    }

    [Fact]
    public async Task JoinGame_WithValidCode_ReturnsPlayerId()
    {
        await using var alice = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");
        await alice.CreateGameAsync();

        await using var bob = await GameTestFixture.CreatePlayerAsync(_factory, "Bob");
        await bob.JoinGameAsync(alice.JoinCode!);

        Assert.NotNull(bob.PlayerId);
        Assert.NotEqual(alice.PlayerId, bob.PlayerId);
    }

    [Fact]
    public async Task JoinGame_PlayerJoinedEventBroadcast()
    {
        await using var alice = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");
        await alice.CreateGameAsync();

        await using var bob = await GameTestFixture.CreatePlayerAsync(_factory, "Bob");
        await bob.JoinGameAsync(alice.JoinCode!);

        var joined = await alice.WaitForPlayerJoinedAsync();
        Assert.Equal(bob.PlayerId, joined.PlayerId);
        Assert.Equal("Bob", joined.PlayerName);
    }

    [Fact]
    public async Task JoinGame_WithInvalidCode_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");

        var ex = await Assert.ThrowsAsync<HubException>(() => player.JoinGameAsync("ZZZZ"));
        Assert.Contains("Game not found", ex.Message);
    }

    [Fact]
    public async Task StartGame_DrawsFirstCard()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();

            var card1 = await players[0].WaitForCardDrawnAsync();
            var card2 = await players[1].WaitForCardDrawnAsync();

            Assert.InRange(card1.Value, 1, 3);
            Assert.Equal(card1.Value, card2.Value);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task StartGame_ByNonCreator_Works()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[1].StartGameAsync();

            var card = await players[0].WaitForCardDrawnAsync();
            Assert.InRange(card.Value, 1, 3);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task FullGame_FinishesAfter18Cards()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        var drawCount = 0;
        for (var i = 0; i < player.TotalCards; i++)
        {
            drawCount++;
            await player.PlaceCardAsync(i % 6);

            if (i < player.TotalCards - 1)
            {
                await player.WaitForCardDrawnAsync();
            }
        }

        await player.WaitForGameFinishedAsync();
        Assert.Equal(18, drawCount);
    }

    [Fact]
    public async Task RestartGame_AfterFinish_StartsNewGame()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        for (var i = 0; i < player.TotalCards; i++)
        {
            await player.PlaceCardAsync(i % 6);
            if (i < player.TotalCards - 1)
            {
                await player.WaitForCardDrawnAsync();
            }
        }

        await player.WaitForGameFinishedAsync();

        await player.RestartGameAsync();
        var restartCard = await player.WaitForGameRestartedAsync();
        Assert.InRange(restartCard.Value, 1, 3);

        for (var i = 0; i < player.TotalCards; i++)
        {
            await player.PlaceCardAsync(i % 6);
            if (i < player.TotalCards - 1)
            {
                await player.WaitForCardDrawnAsync();
            }
        }

        await player.WaitForGameFinishedAsync();
    }

    [Fact]
    public async Task RestartGame_BeforeFinish_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        var ex = await Assert.ThrowsAsync<HubException>(() => player.RestartGameAsync());
        Assert.NotNull(ex);
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
