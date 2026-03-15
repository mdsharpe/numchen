namespace Numchen.Domain.Tests;

public class PlayerBoardTests
{
    [Fact]
    public void NewBoard_IsNotComplete()
    {
        // Arrange
        var board = new PlayerBoard();

        // Act
        var isComplete = board.IsComplete;

        // Assert
        Assert.False(isComplete);
    }

    [Fact]
    public void PlaceCard_AddsToColumn()
    {
        // Arrange
        var board = new PlayerBoard();

        // Act
        board.PlaceCard(new Card(5), 0);

        // Assert
        Assert.Equal(new Card(5), board.PeekColumn(0));
        Assert.Equal(1, board.GetColumnCardCount(0));
    }

    [Fact]
    public void PlaceCard_StacksOnColumn()
    {
        // Arrange
        var board = new PlayerBoard();
        board.PlaceCard(new Card(5), 0);

        // Act
        board.PlaceCard(new Card(10), 0);

        // Assert
        Assert.Equal(new Card(10), board.PeekColumn(0));
        Assert.Equal(2, board.GetColumnCardCount(0));
    }

    [Fact]
    public void PeekColumn_WhenEmpty_ReturnsNull()
    {
        // Arrange
        var board = new PlayerBoard();

        // Act
        var result = board.PeekColumn(0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PlaceCard_InvalidColumn_Throws()
    {
        // Arrange
        var board = new PlayerBoard();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => board.PlaceCard(new Card(1), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.PlaceCard(new Card(1), 6));
    }

    [Fact]
    public void MoveToDestination_WithOne_StartsNewPile()
    {
        // Arrange
        var board = new PlayerBoard();
        board.PlaceCard(new Card(1), 0);

        // Act
        var pileIndex = board.MoveToDestination(0);

        // Assert
        Assert.Equal(0, pileIndex);
        Assert.Null(board.PeekColumn(0));
        Assert.Equal(1, board.GetDestinationPileTopValue(0));
    }

    [Fact]
    public void MoveToDestination_SequentialCards_BuildsPile()
    {
        // Arrange
        var board = new PlayerBoard();
        board.PlaceCard(new Card(1), 0);
        board.MoveToDestination(0);
        board.PlaceCard(new Card(2), 0);
        board.MoveToDestination(0);

        // Act
        board.PlaceCard(new Card(3), 0);
        board.MoveToDestination(0);

        // Assert
        Assert.Equal(3, board.GetDestinationPileTopValue(0));
        Assert.Equal(3, board.GetDestinationPileCardCount(0));
    }

    [Fact]
    public void MoveToDestination_NonSequentialCard_Throws()
    {
        // Arrange
        var board = new PlayerBoard();
        board.PlaceCard(new Card(1), 0);
        board.MoveToDestination(0);
        board.PlaceCard(new Card(3), 0);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.MoveToDestination(0));
    }

    [Fact]
    public void MoveToDestination_EmptyColumn_Throws()
    {
        // Arrange
        var board = new PlayerBoard();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => board.MoveToDestination(0));
    }

    [Fact]
    public void GetCanMoveToDestination_ReturnsCorrectly()
    {
        // Arrange
        var board = new PlayerBoard();
        board.PlaceCard(new Card(5), 0);
        board.PlaceCard(new Card(1), 1);

        // Act
        var canMoveNonOne = board.GetCanMoveToDestination(0);
        var canMoveOne = board.GetCanMoveToDestination(1);

        // Assert
        Assert.False(canMoveNonOne);
        Assert.True(canMoveOne);
    }

    [Fact]
    public void MoveToDestination_MultipleOnes_GoToDifferentPiles()
    {
        // Arrange
        var board = new PlayerBoard();
        board.PlaceCard(new Card(1), 0);
        var pile1 = board.MoveToDestination(0);

        // Act
        board.PlaceCard(new Card(1), 1);
        var pile2 = board.MoveToDestination(1);

        // Assert
        Assert.NotEqual(pile1, pile2);
        Assert.Equal(1, board.GetDestinationPileTopValue(pile1));
        Assert.Equal(1, board.GetDestinationPileTopValue(pile2));
    }

    [Fact]
    public void IsComplete_WhenAllPilesFull_ReturnsTrue()
    {
        // Arrange
        var board = new PlayerBoard();
        for (var pile = 0; pile < PlayerBoard.DestinationPileCount; pile++)
        {
            for (var value = Card.MinValue; value <= Card.MaxValue; value++)
            {
                board.PlaceCard(new Card(value), 0);
                board.MoveToDestination(0);
            }
        }

        // Act
        var isComplete = board.IsComplete;

        // Assert
        Assert.True(isComplete);
    }

    [Fact]
    public void BuriedCard_CannotBeMoved()
    {
        // Arrange
        var board = new PlayerBoard();
        board.PlaceCard(new Card(1), 0);
        board.PlaceCard(new Card(5), 0);

        // Act
        var canMove = board.GetCanMoveToDestination(0);

        // Assert
        Assert.False(canMove);
        Assert.Equal(new Card(5), board.PeekColumn(0));
    }
}
