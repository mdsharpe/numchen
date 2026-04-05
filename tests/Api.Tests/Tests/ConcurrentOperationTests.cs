namespace Numchen.Api.Tests.Tests;

public class ConcurrentOperationTests : IAsyncDisposable
{
    private readonly NumchenWebApplicationFactory _factory = new();

    [Fact]
    public async Task ThreePlayers_SimultaneousPlacement_AllSucceed()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob", "Charlie");
        try
        {
            await players[0].StartGameAsync();
            foreach (var p in players)
            {
                await p.WaitForCardDrawnAsync();
            }

            // All three place simultaneously
            await Task.WhenAll(
                players[0].PlaceCardAsync(0),
                players[1].PlaceCardAsync(1),
                players[2].PlaceCardAsync(2)
            );

            // All should receive next card
            foreach (var p in players)
            {
                await p.WaitForCardDrawnAsync();
            }
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task FullGame_ThreePlayers_ConcurrentPlacementEveryRound()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob", "Charlie");
        try
        {
            await players[0].StartGameAsync();
            foreach (var p in players)
            {
                await p.WaitForCardDrawnAsync();
            }

            var totalCards = players[0].TotalCards;
            for (var round = 0; round < totalCards; round++)
            {
                await Task.WhenAll(
                    players[0].PlaceCardAsync(round % 6),
                    players[1].PlaceCardAsync(round % 6),
                    players[2].PlaceCardAsync(round % 6)
                );

                if (round < totalCards - 1)
                {
                    foreach (var p in players)
                    {
                        await p.WaitForCardDrawnAsync();
                    }
                }
            }

            foreach (var p in players)
            {
                await p.WaitForGameFinishedAsync();
            }
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task MultipleGames_IndependentSessions()
    {
        // Create two independent games
        await using var alice = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");
        await alice.CreateGameAsync();

        await using var bob = await GameTestFixture.CreatePlayerAsync(_factory, "Bob");
        await bob.CreateGameAsync();

        // Start both games
        await alice.StartGameAsync();
        await bob.StartGameAsync();

        var aliceCard = await alice.WaitForCardDrawnAsync();
        var bobCard = await bob.WaitForCardDrawnAsync();

        // Both receive cards independently
        Assert.InRange(aliceCard.Value, 1, 3);
        Assert.InRange(bobCard.Value, 1, 3);

        // Play both games to completion simultaneously
        var aliceTask = PlaySinglePlayerGameAsync(alice);
        var bobTask = PlaySinglePlayerGameAsync(bob);

        await Task.WhenAll(aliceTask, bobTask);
    }

    private static async Task PlaySinglePlayerGameAsync(GamePlayer player)
    {
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

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
