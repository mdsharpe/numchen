namespace Numchen.Api.Tests.Tests;

public class DisconnectTests : IAsyncDisposable
{
    private readonly NumchenWebApplicationFactory _factory;

    public DisconnectTests()
    {
        _factory = new NumchenWebApplicationFactory();
        _factory.HubOptions.DisconnectGracePeriod = TimeSpan.FromSeconds(2);
    }

    [Fact]
    public async Task Disconnect_AndRejoin_WithinGracePeriod_Succeeds()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        var bobPlayerId = players[1].PlayerId!;

        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            // Bob disconnects
            await players[1].StopAsync();
            await players[1].DisposeAsync();

            // Bob reconnects with a new connection before grace period
            var bobReconnected = await GameTestFixture.CreatePlayerAsync(_factory, "Bob");
            players[1] = bobReconnected;

            var rejoinResult = await bobReconnected.RejoinGameAsync(bobPlayerId);

            Assert.True(rejoinResult.GameStarted);
            Assert.False(rejoinResult.GameFinished);
            Assert.NotNull(rejoinResult.CurrentCard);
            Assert.Equal(2, rejoinResult.Players.Count);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task Disconnect_GracePeriodExpires_PlayerRemoved()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            // Bob disconnects
            await players[1].StopAsync();

            // Alice should receive PlayerLeft after grace period (~2s)
            var left = await players[0].WaitForPlayerLeftAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(players[1].PlayerId, left.PlayerId);
            Assert.Equal("Bob", left.PlayerName);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task Disconnect_DuringPlacement_RejoinAndPlace()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        var bobPlayerId = players[1].PlayerId!;

        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            // Bob disconnects before placing
            await players[1].StopAsync();
            await players[1].DisposeAsync();

            // Bob reconnects
            var bobReconnected = await GameTestFixture.CreatePlayerAsync(_factory, "Bob");
            players[1] = bobReconnected;
            await bobReconnected.RejoinGameAsync(bobPlayerId);

            // Alice places
            await players[0].PlaceCardAsync(0);

            // Bob places after rejoin
            await bobReconnected.PlaceCardAsync(0);

            // Game should advance — both get next card
            await players[0].WaitForCardDrawnAsync();
            await bobReconnected.WaitForCardDrawnAsync();
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task Disconnect_GracePeriodExpires_DuringPlacement_UnblocksOtherPlayers()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            // Alice places
            await players[0].PlaceCardAsync(0);

            // Bob disconnects without placing
            await players[1].StopAsync();

            // After grace period, Bob is removed and game should advance
            await players[0].WaitForPlayerLeftAsync(TimeSpan.FromSeconds(5));

            // Alice should get the next card (game unblocked)
            await players[0].WaitForCardDrawnAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task Rejoin_RestoresFullBoardState()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        var bobPlayerId = players[1].PlayerId!;

        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            // Play a few rounds to build up board state
            for (var i = 0; i < 3; i++)
            {
                await players[0].PlaceCardAsync(i % 6);
                await players[1].PlaceCardAsync(i % 6);

                if (i < 2)
                {
                    await players[0].WaitForCardDrawnAsync();
                    await players[1].WaitForCardDrawnAsync();
                }
            }

            // Wait for next card after last placement
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            // Bob disconnects
            await players[1].StopAsync();
            await players[1].DisposeAsync();

            // Bob reconnects
            var bobReconnected = await GameTestFixture.CreatePlayerAsync(_factory, "Bob");
            players[1] = bobReconnected;
            var rejoinResult = await bobReconnected.RejoinGameAsync(bobPlayerId);

            Assert.True(rejoinResult.GameStarted);
            Assert.False(rejoinResult.GameFinished);
            Assert.NotNull(rejoinResult.CurrentCard);
            Assert.Equal(6, rejoinResult.Columns.Length);
            Assert.Equal(6, rejoinResult.Destinations.Length);
            Assert.Equal(2, rejoinResult.Players.Count);

            // Verify columns have cards from the rounds we played
            var totalCardsInColumns = rejoinResult.Columns.Sum(c => c.Length);
            Assert.Equal(3, totalCardsInColumns);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
