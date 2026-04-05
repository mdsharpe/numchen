namespace Numchen.Api.Tests.Tests;

public class FinishingPhaseTests : IAsyncDisposable
{
    private readonly NumchenWebApplicationFactory _factory = new();

    private static async Task PlayAllCardsAsync(GamePlayer player)
    {
        for (var i = 0; i < player.TotalCards; i++)
        {
            await player.PlaceCardAsync(i % 6);
            if (i < player.TotalCards - 1)
            {
                await player.WaitForCardDrawnAsync();
            }
        }
    }

    [Fact]
    public async Task SinglePlayer_FinishingPhaseStarted_BroadcastAfterLastCardPlaced()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await PlayAllCardsAsync(player);

        var deadline = await player.WaitForFinishingPhaseStartedAsync();
        Assert.NotNull(deadline);
        Assert.True(deadline > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task SinglePlayer_GameFinished_AfterFinishingTimeout()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await PlayAllCardsAsync(player);

        await player.WaitForFinishingPhaseStartedAsync();

        // Factory sets FinishingTimeout = 500ms — GameFinished should follow
        await player.WaitForGameFinishedAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SinglePlayer_MoveToDestination_WorksDuringFinishingPhase()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await PlayAllCardsAsync(player);

        await player.WaitForFinishingPhaseStartedAsync();

        // Attempt moves from all columns — any that succeed are valid
        for (var col = 0; col < 6; col++)
        {
            try
            {
                await player.MoveToDestinationAsync(col);
                var scored = await player.WaitForPlayerScoredAsync();
                Assert.True(scored.Score > 0);
            }
            catch (HubException)
            {
                // Column empty or card not dismissable — expected
            }
        }
    }

    [Fact]
    public async Task SinglePlayer_EarlyFinish_SkipsFinishingPhase_WhenBoardAlreadyClear()
    {
        // Aggressively dismiss cards during play so the board is clean when the last card
        // is placed. The server skips the finishing phase entirely in this case and fires
        // GameFinished directly — well before any long timeout would expire.
        var longTimeoutFactory = new NumchenWebApplicationFactory();
        longTimeoutFactory.HubOptions.FinishingTimeout = TimeSpan.FromSeconds(30);

        try
        {
            await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(longTimeoutFactory);

            for (var i = 0; i < player.TotalCards; i++)
            {
                await player.PlaceCardAsync(i % 6);

                var moved = true;
                while (moved)
                {
                    moved = false;
                    for (var col = 0; col < 6; col++)
                    {
                        try
                        {
                            await player.MoveToDestinationAsync(col);
                            await player.WaitForPlayerScoredAsync();
                            moved = true;
                        }
                        catch (HubException) { }
                    }
                }

                if (i < player.TotalCards - 1)
                {
                    await player.WaitForCardDrawnAsync();
                }
            }

            // Board is clean — server must have skipped finishing and sent GameFinished directly,
            // well before the 30-second timer.
            await player.WaitForGameFinishedAsync(TimeSpan.FromSeconds(3));
        }
        finally
        {
            await longTimeoutFactory.DisposeAsync();
        }
    }

    [Fact]
    public async Task SinglePlayer_EarlyFinish_FastForward_WhenAllCardsDismissedDuringFinishing()
    {
        // Use a long timeout so GameFinished can only arrive early via fast-forward,
        // not via timer. Play without dismissing during play to guarantee dismissable
        // cards remain at game end. Then dismiss them all in the finishing phase.
        var longTimeoutFactory = new NumchenWebApplicationFactory();
        longTimeoutFactory.HubOptions.FinishingTimeout = TimeSpan.FromSeconds(30);

        try
        {
            await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(longTimeoutFactory);

            // Play without dismissing — maximises leftover dismissable cards at game end
            await PlayAllCardsAsync(player);

            // Board may or may not have dismissable cards depending on random draw order.
            // If the server skipped finishing (board was clean), GameFinished arrives now.
            // If finishing phase started, exhaust all dismissable cards to trigger fast-forward.
            var gameFinishedTask = player.WaitForGameFinishedAsync(TimeSpan.FromSeconds(5));

            try
            {
                await player.WaitForFinishingPhaseStartedAsync(TimeSpan.FromMilliseconds(500));

                var stillMoving = true;
                while (stillMoving)
                {
                    stillMoving = false;
                    for (var col = 0; col < 6; col++)
                    {
                        try
                        {
                            await player.MoveToDestinationAsync(col);
                            await player.WaitForPlayerScoredAsync();
                            stillMoving = true;
                        }
                        catch (HubException) { }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Board was already clean — server skipped finishing phase entirely
            }

            await gameFinishedTask;
        }
        finally
        {
            await longTimeoutFactory.DisposeAsync();
        }
    }

    [Fact]
    public async Task SinglePlayer_MoveToDestination_AfterGameFinished_Throws()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await PlayAllCardsAsync(player);
        await player.WaitForFinishingPhaseStartedAsync();
        await player.WaitForGameFinishedAsync(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<HubException>(() => player.MoveToDestinationAsync(0));
    }

    [Fact]
    public async Task TwoPlayers_BothReceiveFinishingPhaseStarted()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            await GameTestFixture.PlayFullGameAsync(players.ToArray());

            var deadline0 = await players[0].WaitForFinishingPhaseStartedAsync();
            var deadline1 = await players[1].WaitForFinishingPhaseStartedAsync();

            Assert.NotNull(deadline0);
            Assert.NotNull(deadline1);
            Assert.Equal(deadline0, deadline1);
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task TwoPlayers_BothReceiveGameFinished_AfterFinishingTimeout()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            await GameTestFixture.PlayFullGameAsync(players.ToArray());

            await players[0].WaitForFinishingPhaseStartedAsync();
            await players[1].WaitForFinishingPhaseStartedAsync();

            await players[0].WaitForGameFinishedAsync(TimeSpan.FromSeconds(5));
            await players[1].WaitForGameFinishedAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task RestartGame_AfterFinishingPhase_StartsNewGame()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await PlayAllCardsAsync(player);
        await player.WaitForFinishingPhaseStartedAsync();
        await player.WaitForGameFinishedAsync(TimeSpan.FromSeconds(5));

        await player.RestartGameAsync();
        var restartCard = await player.WaitForGameRestartedAsync();
        Assert.InRange(restartCard.Value, 1, 3);
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
