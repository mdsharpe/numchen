namespace Numchen.Domain;

public class Deck
{
    public const int DefaultSetCount = 6;

    private readonly Stack<Card> _cards;
    private readonly Random _rng;
    private readonly int _setCount;
    private readonly int _maxCardValue;

    public int SetCount => _setCount;
    public int MaxCardValue => _maxCardValue;

    public Deck(Random? random = null, int setCount = DefaultSetCount, int maxCardValue = Card.MaxValue)
    {
        _rng = random ?? Random.Shared;
        _setCount = setCount;
        _maxCardValue = maxCardValue;
        _cards = new Stack<Card>();
        Reshuffle();
    }

    public void Reshuffle()
    {
        _cards.Clear();

        var cards = new List<Card>();

        for (var set = 0; set < _setCount; set++)
        {
            for (var value = Card.MinValue; value <= _maxCardValue; value++)
            {
                cards.Add(new Card(value));
            }
        }

        foreach (var card in cards.OrderBy(_ => _rng.Next()))
        {
            _cards.Push(card);
        }
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
