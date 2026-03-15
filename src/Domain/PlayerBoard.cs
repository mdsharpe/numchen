namespace Numchen.Domain;

public class PlayerBoard
{
    public const int ColumnCount = 6;
    public const int DestinationPileCount = 6;

    private readonly Stack<Card>[] _columns;
    private readonly Stack<Card>[] _destinationPiles;

    public PlayerBoard()
    {
        _columns = new Stack<Card>[ColumnCount];
        for (var i = 0; i < ColumnCount; i++)
        {
            _columns[i] = new Stack<Card>();
        }

        _destinationPiles = new Stack<Card>[DestinationPileCount];
        for (var i = 0; i < DestinationPileCount; i++)
        {
            _destinationPiles[i] = new Stack<Card>();
        }
    }

    public bool IsComplete => _destinationPiles.All(p => p.Count == Card.MaxValue);

    public void PlaceCard(Card card, int columnIndex)
    {
        ValidateColumnIndex(columnIndex);
        _columns[columnIndex].Push(card);
    }

    public Card? PeekColumn(int columnIndex)
    {
        ValidateColumnIndex(columnIndex);
        return _columns[columnIndex].Count > 0 ? _columns[columnIndex].Peek() : null;
    }

    public int GetColumnCardCount(int columnIndex)
    {
        ValidateColumnIndex(columnIndex);
        return _columns[columnIndex].Count;
    }

    public bool GetCanMoveToDestination(int columnIndex)
    {
        ValidateColumnIndex(columnIndex);

        if (_columns[columnIndex].Count == 0)
        {
            return false;
        }

        var card = _columns[columnIndex].Peek();
        return FindDestinationPileFor(card) is not null;
    }

    public int MoveToDestination(int columnIndex)
    {
        ValidateColumnIndex(columnIndex);

        if (_columns[columnIndex].Count == 0)
        {
            throw new InvalidOperationException("Column is empty.");
        }

        var card = _columns[columnIndex].Peek();
        var pileIndex = FindDestinationPileFor(card)
            ?? throw new InvalidOperationException(
                $"Card {card.Value} cannot be placed on any destination pile.");

        _columns[columnIndex].Pop();
        _destinationPiles[pileIndex].Push(card);

        return pileIndex;
    }

    public int GetDestinationPileTopValue(int pileIndex)
    {
        ValidateDestinationPileIndex(pileIndex);
        return _destinationPiles[pileIndex].Count > 0
            ? _destinationPiles[pileIndex].Peek().Value
            : 0;
    }

    public int GetDestinationPileCardCount(int pileIndex)
    {
        ValidateDestinationPileIndex(pileIndex);
        return _destinationPiles[pileIndex].Count;
    }

    private int? FindDestinationPileFor(Card card)
    {
        if (card.Value == Card.MinValue)
        {
            // A '1' can go on any empty destination pile
            for (var i = 0; i < DestinationPileCount; i++)
            {
                if (_destinationPiles[i].Count == 0)
                {
                    return i;
                }
            }
        }
        else
        {
            // Other cards must go on a pile whose top card is exactly one less
            for (var i = 0; i < DestinationPileCount; i++)
            {
                if (_destinationPiles[i].Count > 0
                    && _destinationPiles[i].Peek().Value == card.Value - 1)
                {
                    return i;
                }
            }
        }

        return null;
    }

    private static void ValidateColumnIndex(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }
    }

    private static void ValidateDestinationPileIndex(int pileIndex)
    {
        if (pileIndex < 0 || pileIndex >= DestinationPileCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pileIndex));
        }
    }
}
