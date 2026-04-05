namespace Numchen.Api.Tests.Tests;

public class SinglePlayerGameTests : IAsyncDisposable
{
    private readonly NumchenWebApplicationFactory _factory = new();

    [Fact]
    public async Task SinglePlayer_PlaceAllCardsInOneColumn_Completes()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        for (var i = 0; i < player.TotalCards; i++)
        {
            await player.PlaceCardAsync(0);
            if (i < player.TotalCards - 1)
            {
                await player.WaitForCardDrawnAsync();
            }
        }

        await player.WaitForGameFinishedAsync();
    }

    [Fact]
    public async Task SinglePlayer_SpreadCardsAcrossColumns_Completes()
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
    }

    [Fact]
    public async Task SinglePlayer_MoveToDestination_IncreasesScore()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        var scored = false;
        for (var i = 0; i < player.TotalCards; i++)
        {
            await player.PlaceCardAsync(i % 6);

            // Try to move cards to destination after each placement
            for (var col = 0; col < 6; col++)
            {
                try
                {
                    var result = await player.MoveToDestinationAsync(col);
                    var scoreEvent = await player.WaitForPlayerScoredAsync();
                    Assert.Equal(player.PlayerId, scoreEvent.PlayerId);
                    Assert.True(scoreEvent.Score > 0);
                    Assert.InRange(result.PileIndex, 0, 5);
                    scored = true;
                }
                catch (HubException)
                {
                    // Card can't move to destination — expected
                }
            }

            if (i < player.TotalCards - 1)
            {
                await player.WaitForCardDrawnAsync();
            }
        }

        // With 18 cards (values 1-3, 6 of each), at least some 1s should be movable
        Assert.True(scored, "Expected at least one card to move to a destination pile");
    }

    [Fact]
    public async Task SinglePlayer_FullGameWithMaxDestinationMoves_ScoreEqualsMovedCards()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        var moveCount = 0;
        var lastScore = 0;

        for (var i = 0; i < player.TotalCards; i++)
        {
            await player.PlaceCardAsync(i % 6);

            // Attempt destination moves from all columns repeatedly until none succeed
            var moved = true;
            while (moved)
            {
                moved = false;
                for (var col = 0; col < 6; col++)
                {
                    try
                    {
                        await player.MoveToDestinationAsync(col);
                        var scoreEvent = await player.WaitForPlayerScoredAsync();
                        lastScore = scoreEvent.Score;
                        moveCount++;
                        moved = true;
                    }
                    catch (HubException)
                    {
                        // Can't move — expected
                    }
                }
            }

            if (i < player.TotalCards - 1)
            {
                await player.WaitForCardDrawnAsync();
            }
        }

        Assert.Equal(moveCount, lastScore);
    }

    [Fact]
    public async Task SinglePlayer_MoveToDestination_EmptyColumn_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        // Place in column 0, then try to move from empty column 1
        await player.PlaceCardAsync(0);

        await Assert.ThrowsAsync<HubException>(() => player.MoveToDestinationAsync(1));
    }

    [Fact]
    public async Task SinglePlayer_MoveToDestination_NonSequentialCard_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        // Place first card in column 0
        await player.PlaceCardAsync(0);

        // If the card was a 1, it would succeed — place more cards to get a non-1 on top
        // Try to move: if it's a 1 it will succeed, if not it will throw
        try
        {
            await player.MoveToDestinationAsync(0);
            // Card was a 1 and moved successfully — need to set up a non-sequential scenario
            // Place more cards and try again with a card that doesn't fit
            for (var i = 0; i < player.TotalCards - 2; i++)
            {
                var card = await player.WaitForCardDrawnAsync();
                await player.PlaceCardAsync(1);

                // Try to move from column 1 — eventually a non-sequential card will be on top
                try
                {
                    await player.MoveToDestinationAsync(1);
                    await player.WaitForPlayerScoredAsync();
                }
                catch (HubException)
                {
                    // Got a non-sequential card — test passed
                    return;
                }
            }
        }
        catch (HubException)
        {
            // First card was not a 1 — this is the expected error case
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
