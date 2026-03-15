namespace Numchen.Domain;

public class Deck
{
    public const int SetCount = 6;

    private readonly Stack<Card> _cards;

    public Deck(Random? random = null)
    {
        var cards = new List<Card>();

        for (var set = 0; set < SetCount; set++)
        {
            for (var value = Card.MinValue; value <= Card.MaxValue; value++)
            {
                cards.Add(new Card(value));
            }
        }

        var rng = random ?? Random.Shared;

        _cards = new Stack<Card>(cards.OrderBy(_ => rng.Next()));
    }

    public int RemainingCount => _cards.Count;

    public bool IsEmpty => _cards.Count == 0;

    public Card Draw()
    {
        if (_cards.Count == 0)
        {
            throw new InvalidOperationException("The deck is empty.");
        }

        return _cards.Pop();
    }
}
