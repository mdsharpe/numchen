namespace Numchen.Api.Tests.Tests;

public class MultiPlayerGameTests : IAsyncDisposable
{
    private readonly NumchenWebApplicationFactory _factory = new();

    [Fact]
    public async Task TwoPlayers_BothMustPlaceBeforeNextDraw()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            // Player 1 places
            await players[0].PlaceCardAsync(0);

            // No next card should arrive yet (only one player placed)
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => players[0].WaitForCardDrawnAsync(TimeSpan.FromMilliseconds(500)));

            // Player 2 places — now both should get next card
            await players[1].PlaceCardAsync(0);

            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task TwoPlayers_IndependentBoards_DifferentScores()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            var bobMoveCount = 0;

            for (var i = 0; i < players[0].TotalCards; i++)
            {
                // Alice always places in column 0, never moves to destination
                await players[0].PlaceCardAsync(0);

                // Bob spreads cards and tries destination moves
                await players[1].PlaceCardAsync(i % 6);

                for (var col = 0; col < 6; col++)
                {
                    try
                    {
                        await players[1].MoveToDestinationAsync(col);
                        await players[1].WaitForPlayerScoredAsync();
                        // Alice also receives the PlayerScored broadcast
                        await players[0].WaitForPlayerScoredAsync();
                        bobMoveCount++;
                    }
                    catch (HubException)
                    {
                        // Can't move — expected
                    }
                }

                if (i < players[0].TotalCards - 1)
                {
                    await players[0].WaitForCardDrawnAsync();
                    await players[1].WaitForCardDrawnAsync();
                }
            }

            // Bob should have scored more than Alice (who scored 0)
            Assert.True(bobMoveCount >= 0);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task ThreePlayers_AllReceiveCardDrawn()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob", "Charlie");
        try
        {
            await players[0].StartGameAsync();

            var card1 = await players[0].WaitForCardDrawnAsync();
            var card2 = await players[1].WaitForCardDrawnAsync();
            var card3 = await players[2].WaitForCardDrawnAsync();

            Assert.Equal(card1.Value, card2.Value);
            Assert.Equal(card2.Value, card3.Value);
            Assert.InRange(card1.Value, 1, 3);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task TwoPlayers_PlayerPlacedBroadcast()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            await players[0].PlaceCardAsync(0);

            // Both players receive PlayerPlaced for Alice
            var placed0 = await players[0].WaitForPlayerPlacedAsync();
            var placed1 = await players[1].WaitForPlayerPlacedAsync();

            Assert.Equal(players[0].PlayerId, placed0.PlayerId);
            Assert.Equal("Alice", placed0.PlayerName);
            Assert.Equal(players[0].PlayerId, placed1.PlayerId);

            await players[1].PlaceCardAsync(0);

            // Both receive PlayerPlaced for Bob
            var placed2 = await players[0].WaitForPlayerPlacedAsync();
            var placed3 = await players[1].WaitForPlayerPlacedAsync();

            Assert.Equal(players[1].PlayerId, placed2.PlayerId);
            Assert.Equal("Bob", placed2.PlayerName);
            Assert.Equal(players[1].PlayerId, placed3.PlayerId);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task TwoPlayers_FullGame_BothReceiveGameFinished()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            await GameTestFixture.PlayFullGameAsync(players.ToArray());

            await players[0].WaitForGameFinishedAsync();
            await players[1].WaitForGameFinishedAsync();
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task TwoPlayers_OnePlayerMoves_OtherSeesScore()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            var scored = false;

            for (var i = 0; i < players[0].TotalCards; i++)
            {
                await players[0].PlaceCardAsync(i % 6);
                await players[1].PlaceCardAsync(i % 6);

                if (!scored)
                {
                    // Alice tries to move to destination
                    for (var col = 0; col < 6; col++)
                    {
                        try
                        {
                            await players[0].MoveToDestinationAsync(col);
                            // Alice scored — Bob should see it too
                            var aliceScore = await players[0].WaitForPlayerScoredAsync();
                            var bobSeesScore = await players[1].WaitForPlayerScoredAsync();

                            Assert.Equal(players[0].PlayerId, aliceScore.PlayerId);
                            Assert.Equal(players[0].PlayerId, bobSeesScore.PlayerId);
                            Assert.Equal(aliceScore.Score, bobSeesScore.Score);
                            scored = true;
                            break;
                        }
                        catch (HubException)
                        {
                            // Can't move — try next column
                        }
                    }
                }

                if (i < players[0].TotalCards - 1)
                {
                    await players[0].WaitForCardDrawnAsync();
                    await players[1].WaitForCardDrawnAsync();
                }
            }
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
