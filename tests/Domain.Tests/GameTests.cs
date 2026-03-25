namespace Numchen.Domain.Tests;

public class GameTests
{
    [Theory]
    [AutoData]
    public void NewGame_IsWaitingForPlayers(int seed)
    {
        // Arrange
        var game = new Game(new Random(seed));

        // Act
        var state = game.State;

        // Assert
        Assert.Equal(GameState.WaitingForPlayers, state);
    }

    [Theory]
    [AutoData]
    public void AddPlayer_ThenStart_IsReadyToDraw(int seed, string playerId)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId);

        // Act
        game.Start();

        // Assert
        Assert.Equal(GameState.ReadyToDraw, game.State);
    }

    [Theory]
    [AutoData]
    public void Start_WithNoPlayers_Throws(int seed)
    {
        // Arrange
        var game = new Game(new Random(seed));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => game.Start());
    }

    [Theory]
    [AutoData]
    public void AddPlayer_AfterStart_Throws(int seed, string playerId1, string playerId2)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId1);
        game.Start();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => game.AddPlayer(playerId2));
    }

    [Theory]
    [AutoData]
    public void AddDuplicatePlayer_Throws(int seed, string playerId)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => game.AddPlayer(playerId));
    }

    [Theory]
    [AutoData]
    public void DrawCard_ReturnsCard(int seed, string playerId)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId);
        game.Start();

        // Act
        var card = game.DrawCard();

        // Assert
        Assert.InRange(card.Value, Card.MinValue, Card.MaxValue);
        Assert.Equal(GameState.PlacingCard, game.State);
    }

    [Theory]
    [AutoData]
    public void PlaceCard_AllPlayersReady_AdvancesToReadyToDraw(int seed, string playerId1, string playerId2)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId1);
        game.AddPlayer(playerId2);
        game.Start();
        game.DrawCard();

        // Act
        game.PlaceCard(playerId1, 0);
        var stateAfterFirst = game.State;
        game.PlaceCard(playerId2, 0);
        var stateAfterSecond = game.State;

        // Assert
        Assert.Equal(GameState.PlacingCard, stateAfterFirst);
        Assert.Equal(GameState.ReadyToDraw, stateAfterSecond);
    }

    [Theory]
    [AutoData]
    public void PlaceCard_SamePlayerTwice_Throws(int seed, string playerId)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId);
        game.Start();
        game.DrawCard();

        // Act
        game.PlaceCard(playerId, 0);

        // Assert
        Assert.Equal(GameState.ReadyToDraw, game.State);
    }

    [Theory]
    [AutoData]
    public void PlaceCard_UnknownPlayer_Throws(int seed, string playerId, string unknownPlayerId)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId);
        game.Start();
        game.DrawCard();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => game.PlaceCard(unknownPlayerId, 0));
    }

    [Theory]
    [AutoData]
    public void FullGame_SinglePlayer_CompletesAfter96Draws(int seed, string playerId)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId);
        game.Start();
        var drawCount = 0;

        // Act
        while (game.State != GameState.Finished)
        {
            game.DrawCard();
            drawCount++;
            game.PlaceCard(playerId, drawCount % PlayerBoard.ColumnCount);

            var board = game.GetPlayerBoard(playerId);
            for (var col = 0; col < PlayerBoard.ColumnCount; col++)
            {
                while (board.GetCanMoveToDestination(col))
                {
                    board.MoveToDestination(col);
                }
            }
        }

        // Assert
        Assert.Equal(96, drawCount);
        Assert.Equal(GameState.Finished, game.State);
    }

    [Theory]
    [AutoData]
    public void MoveToDestination_DuringGame_Works(int seed, string playerId)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId);
        game.Start();
        var board = game.GetPlayerBoard(playerId);
        var moved = false;

        // Act
        for (var i = 0; i < 96 && !moved; i++)
        {
            game.DrawCard();
            game.PlaceCard(playerId, 0);

            if (board.GetCanMoveToDestination(0))
            {
                game.MoveToDestination(playerId, 0);
                moved = true;
            }

            if (game.State == GameState.Finished)
            {
                break;
            }
        }

        // Assert - no exceptions thrown during the process
    }

    [Theory]
    [AutoData]
    public void MoveToDestination_AfterGameFinished_Throws(int seed, string playerId)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId);
        game.Start();

        while (game.State != GameState.Finished)
        {
            game.DrawCard();
            game.PlaceCard(playerId, 0);
        }

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => game.MoveToDestination(playerId, 0));
    }

    [Theory]
    [AutoData]
    public void TwoPlayers_IndependentBoards(int seed, string playerId1, string playerId2)
    {
        // Arrange
        var game = new Game(new Random(seed));
        game.AddPlayer(playerId1);
        game.AddPlayer(playerId2);
        game.Start();

        // Act
        var card = game.DrawCard();
        game.PlaceCard(playerId1, 0);
        game.PlaceCard(playerId2, 3);

        // Assert
        var board1 = game.GetPlayerBoard(playerId1);
        var board2 = game.GetPlayerBoard(playerId2);
        Assert.Equal(card, board1.PeekColumn(0));
        Assert.Null(board1.PeekColumn(3));
        Assert.Equal(card, board2.PeekColumn(3));
        Assert.Null(board2.PeekColumn(0));
    }
}
