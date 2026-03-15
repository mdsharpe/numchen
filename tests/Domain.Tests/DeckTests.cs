namespace Numchen.Domain.Tests;

public class DeckTests
{
    [Theory]
    [AutoData]
    public void NewDeck_Has96Cards(int seed)
    {
        // Arrange
        var deck = new Deck(new Random(seed));

        // Act
        var count = deck.RemainingCount;

        // Assert
        Assert.Equal(96, count);
    }

    [Theory]
    [AutoData]
    public void Draw_ReturnsCardAndReducesCount(int seed)
    {
        // Arrange
        var deck = new Deck(new Random(seed));

        // Act
        var card = deck.Draw();

        // Assert
        Assert.InRange(card.Value, Card.MinValue, Card.MaxValue);
        Assert.Equal(95, deck.RemainingCount);
    }

    [Theory]
    [AutoData]
    public void DrawAll_Returns96CardsWithCorrectDistribution(int seed)
    {
        // Arrange
        var deck = new Deck(new Random(seed));
        var cards = new List<Card>();

        // Act
        while (!deck.IsEmpty)
        {
            cards.Add(deck.Draw());
        }

        // Assert
        Assert.Equal(96, cards.Count);
        for (var value = Card.MinValue; value <= Card.MaxValue; value++)
        {
            var count = cards.Count(c => c.Value == value);
            Assert.Equal(Deck.SetCount, count);
        }
    }

    [Theory]
    [AutoData]
    public void Draw_WhenEmpty_Throws(int seed)
    {
        // Arrange
        var deck = new Deck(new Random(seed));
        for (var i = 0; i < 96; i++)
        {
            deck.Draw();
        }

        // Act & Assert
        Assert.True(deck.IsEmpty);
        Assert.Throws<InvalidOperationException>(() => deck.Draw());
    }

    [Theory]
    [AutoData]
    public void DifferentSeeds_ProduceDifferentOrders(int seed1, int seed2)
    {
        // Arrange
        var deck1 = new Deck(new Random(seed1));
        var deck2 = new Deck(new Random(seed2));
        var cards1 = new List<int>();
        var cards2 = new List<int>();

        // Act
        for (var i = 0; i < 10; i++)
        {
            cards1.Add(deck1.Draw().Value);
            cards2.Add(deck2.Draw().Value);
        }

        // Assert
        Assert.NotEqual(cards1, cards2);
    }
}
