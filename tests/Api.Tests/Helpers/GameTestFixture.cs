namespace Numchen.Api.Tests.Helpers;

public static class GameTestFixture
{
    public static async Task<GamePlayer> CreatePlayerAsync(NumchenWebApplicationFactory factory, string name)
    {
        var connection = factory.CreateHubConnection();
        var player = new GamePlayer(connection, name);
        await player.ConnectAsync();
        return player;
    }

    public static async Task<GamePlayer> CreateAndStartSinglePlayerGameAsync(NumchenWebApplicationFactory factory, string name = "Alice")
    {
        var player = await CreatePlayerAsync(factory, name);
        await player.CreateGameAsync();
        await player.StartGameAsync();
        await player.WaitForCardDrawnAsync();
        return player;
    }

    public static async Task<List<GamePlayer>> CreateAndJoinGameAsync(NumchenWebApplicationFactory factory, params string[] names)
    {
        var players = new List<GamePlayer>();

        var creator = await CreatePlayerAsync(factory, names[0]);
        await creator.CreateGameAsync();
        players.Add(creator);

        for (var i = 1; i < names.Length; i++)
        {
            var joiner = await CreatePlayerAsync(factory, names[i]);
            await joiner.JoinGameAsync(creator.JoinCode!);
            players.Add(joiner);
        }

        return players;
    }

    public static async Task PlayFullGameAsync(params GamePlayer[] players)
    {
        var totalCards = players[0].TotalCards;

        for (var round = 0; round < totalCards; round++)
        {
            foreach (var player in players)
            {
                await player.PlaceCardAsync(round % 6);
            }

            if (round < totalCards - 1)
            {
                foreach (var player in players)
                {
                    await player.WaitForCardDrawnAsync();
                }
            }
        }
    }

    public static async Task DisposeAllAsync(params GamePlayer[] players)
    {
        foreach (var player in players)
        {
            await player.DisposeAsync();
        }
    }
}
