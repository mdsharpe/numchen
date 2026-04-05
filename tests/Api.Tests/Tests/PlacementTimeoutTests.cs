namespace Numchen.Api.Tests.Tests;

public class PlacementTimeoutTests : IAsyncDisposable
{
    private readonly NumchenWebApplicationFactory _factory;

    public PlacementTimeoutTests()
    {
        _factory = new NumchenWebApplicationFactory();
        _factory.HubOptions.PlacementTimeout = TimeSpan.FromSeconds(2);
    }

    [Fact]
    public async Task PlacementTimeout_SinglePlayer_AutoPlacesAndAdvances()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        // Don't place — wait for auto-placement
        var autoPlacedColumn = await player.WaitForCardAutoPlacedAsync(TimeSpan.FromSeconds(5));
        Assert.InRange(autoPlacedColumn, 0, 5);

        // Game should advance to next card
        await player.WaitForCardDrawnAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PlacementTimeout_TwoPlayers_OnlyUnreadyPlayerAutoPlaced()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            // Alice places, Bob does not
            await players[0].PlaceCardAsync(0);

            // Bob should get auto-placed
            var autoColumn = await players[1].WaitForCardAutoPlacedAsync(TimeSpan.FromSeconds(5));
            Assert.InRange(autoColumn, 0, 5);

            // Both should get next card
            await players[0].WaitForCardDrawnAsync(TimeSpan.FromSeconds(5));
            await players[1].WaitForCardDrawnAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task PlacementTimeout_AutoPlacesToColumnWithFewestCards()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        // Place first few cards manually to create asymmetric layout
        // Put 2 cards in column 0, 1 card in column 1, leave columns 2-5 empty
        await player.PlaceCardAsync(0);
        await player.WaitForCardDrawnAsync();
        await player.PlaceCardAsync(0);
        await player.WaitForCardDrawnAsync();
        await player.PlaceCardAsync(1);
        await player.WaitForCardDrawnAsync();

        // Now let timeout fire — should auto-place in one of the empty columns (2-5)
        var autoColumn = await player.WaitForCardAutoPlacedAsync(TimeSpan.FromSeconds(5));
        Assert.InRange(autoColumn, 2, 5);
    }

    [Fact]
    public async Task PlacementTimeout_FullGameViaTimeouts_Completes()
    {
        // Use a very short timeout for this test
        var fastFactory = new NumchenWebApplicationFactory();
        fastFactory.HubOptions.PlacementTimeout = TimeSpan.FromMilliseconds(500);

        try
        {
            await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(fastFactory);

            // Never place manually — let all rounds auto-place
            await player.WaitForGameFinishedAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            await fastFactory.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
