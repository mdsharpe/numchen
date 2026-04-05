namespace Numchen.Api.Tests.Tests;

public class ErrorCaseTests : IAsyncDisposable
{
    private readonly NumchenWebApplicationFactory _factory = new();

    [Fact]
    public async Task JoinGame_InvalidCode_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");

        var ex = await Assert.ThrowsAsync<HubException>(() => player.JoinGameAsync("XXXX"));
        Assert.Contains("Game not found", ex.Message);
    }

    [Fact]
    public async Task PlaceCard_WhenNotInGame_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");

        var ex = await Assert.ThrowsAsync<HubException>(() => player.PlaceCardAsync(0));
        Assert.Contains("Not in a game", ex.Message);
    }

    [Fact]
    public async Task PlaceCard_InvalidColumnIndex_ThrowsException()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await Assert.ThrowsAsync<HubException>(() => player.PlaceCardAsync(6));
        await Assert.ThrowsAsync<HubException>(() => player.PlaceCardAsync(-1));
    }

    [Fact]
    public async Task PlaceCard_AlreadyPlacedThisRound_ThrowsHubException()
    {
        var players = await GameTestFixture.CreateAndJoinGameAsync(_factory, "Alice", "Bob");
        try
        {
            await players[0].StartGameAsync();
            await players[0].WaitForCardDrawnAsync();
            await players[1].WaitForCardDrawnAsync();

            await players[0].PlaceCardAsync(0);

            await Assert.ThrowsAsync<HubException>(() => players[0].PlaceCardAsync(1));
        }
        finally
        {
            await GameTestFixture.DisposeAllAsync(players.ToArray());
        }
    }

    [Fact]
    public async Task MoveToDestination_WhenNotInGame_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");

        var ex = await Assert.ThrowsAsync<HubException>(() => player.MoveToDestinationAsync(0));
        Assert.Contains("Not in a game", ex.Message);
    }

    [Fact]
    public async Task MoveToDestination_InvalidColumnIndex_ThrowsException()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await Assert.ThrowsAsync<HubException>(() => player.MoveToDestinationAsync(6));
        await Assert.ThrowsAsync<HubException>(() => player.MoveToDestinationAsync(-1));
    }

    [Fact]
    public async Task StartGame_WhenAlreadyStarted_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await Assert.ThrowsAsync<HubException>(() => player.StartGameAsync());
    }

    [Fact]
    public async Task JoinGame_AfterGameStarted_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreateAndStartSinglePlayerGameAsync(_factory);

        await using var latecomer = await GameTestFixture.CreatePlayerAsync(_factory, "Latecomer");

        await Assert.ThrowsAsync<HubException>(() => latecomer.JoinGameAsync(player.JoinCode!));
    }

    [Fact]
    public async Task RejoinGame_WithInvalidPlayerId_ThrowsHubException()
    {
        await using var player = await GameTestFixture.CreatePlayerAsync(_factory, "Alice");

        var ex = await Assert.ThrowsAsync<HubException>(() => player.RejoinGameAsync("nonexistent-id"));
        Assert.Contains("Game not found", ex.Message);
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}
