namespace Numchen.Domain;

public class Game
{
    private readonly Dictionary<string, PlayerBoard> _players = new();
    private readonly Deck _deck;
    private readonly HashSet<string> _readyPlayers = new();
    private Card? _currentCard;

    public Game(Random? random = null)
    {
        _deck = new Deck(random);
        State = GameState.WaitingForPlayers;
    }

    public GameState State { get; private set; }

    public Card? CurrentCard => _currentCard;

    public int RemainingCards => _deck.RemainingCount;

    public IReadOnlyCollection<string> PlayerIds => _players.Keys;

    public IReadOnlySet<string> ReadyPlayers => _readyPlayers;

    public void AddPlayer(string playerId)
    {
        if (State != GameState.WaitingForPlayers)
        {
            throw new InvalidOperationException("Cannot add players after the game has started.");
        }

        if (_players.ContainsKey(playerId))
        {
            throw new InvalidOperationException($"Player '{playerId}' is already in the game.");
        }

        _players[playerId] = new PlayerBoard();
    }

    public void Start()
    {
        if (State != GameState.WaitingForPlayers)
        {
            throw new InvalidOperationException("Game has already started.");
        }

        if (_players.Count == 0)
        {
            throw new InvalidOperationException("Cannot start a game with no players.");
        }

        State = GameState.ReadyToDraw;
    }

    public Card DrawCard()
    {
        if (State != GameState.ReadyToDraw)
        {
            throw new InvalidOperationException("Not ready to draw a card.");
        }

        _currentCard = _deck.Draw();
        _readyPlayers.Clear();
        State = GameState.PlacingCard;

        return _currentCard.Value;
    }

    public void PlaceCard(string playerId, int columnIndex)
    {
        if (State != GameState.PlacingCard)
        {
            throw new InvalidOperationException("No card to place.");
        }

        if (!_players.TryGetValue(playerId, out var board))
        {
            throw new InvalidOperationException($"Player '{playerId}' is not in the game.");
        }

        if (_readyPlayers.Contains(playerId))
        {
            throw new InvalidOperationException($"Player '{playerId}' has already placed this card.");
        }

        board.PlaceCard(_currentCard!.Value, columnIndex);
        _readyPlayers.Add(playerId);

        if (_readyPlayers.Count == _players.Count)
        {
            if (_deck.IsEmpty)
            {
                State = GameState.Finished;
            }
            else
            {
                State = GameState.ReadyToDraw;
            }

            _currentCard = null;
        }
    }

    public IReadOnlyList<(string PlayerId, int ColumnIndex)> AutoPlaceForUnreadyPlayers()
    {
        if (State != GameState.PlacingCard)
        {
            throw new InvalidOperationException("No card to place.");
        }

        var placements = new List<(string PlayerId, int ColumnIndex)>();
        var unreadyPlayers = _players.Keys.Where(id => !_readyPlayers.Contains(id)).ToList();
        foreach (var playerId in unreadyPlayers)
        {
            var board = _players[playerId];
            var columnIndex = GetColumnWithFewestCards(board);
            PlaceCard(playerId, columnIndex);
            placements.Add((playerId, columnIndex));
        }

        return placements;
    }

    private static int GetColumnWithFewestCards(PlayerBoard board)
    {
        var bestIndex = 0;
        var bestCount = board.GetColumnCardCount(0);

        for (var i = 1; i < PlayerBoard.ColumnCount; i++)
        {
            var count = board.GetColumnCardCount(i);
            if (count < bestCount)
            {
                bestIndex = i;
                bestCount = count;
            }
        }

        return bestIndex;
    }

    public void RemovePlayer(string playerId)
    {
        if (!_players.Remove(playerId))
        {
            throw new InvalidOperationException($"Player '{playerId}' is not in the game.");
        }

        _readyPlayers.Remove(playerId);

        if (_players.Count == 0)
        {
            State = GameState.Finished;
            _currentCard = null;
            return;
        }

        if (State == GameState.PlacingCard && _readyPlayers.Count == _players.Count)
        {
            if (_deck.IsEmpty)
            {
                State = GameState.Finished;
            }
            else
            {
                State = GameState.ReadyToDraw;
            }

            _currentCard = null;
        }
    }

    public int MoveToDestination(string playerId, int columnIndex)
    {
        if (!_players.TryGetValue(playerId, out var board))
        {
            throw new InvalidOperationException($"Player '{playerId}' is not in the game.");
        }

        return board.MoveToDestination(columnIndex);
    }

    public PlayerBoard GetPlayerBoard(string playerId)
    {
        if (!_players.TryGetValue(playerId, out var board))
        {
            throw new InvalidOperationException($"Player '{playerId}' is not in the game.");
        }

        return board;
    }
}

public enum GameState
{
    WaitingForPlayers,
    ReadyToDraw,
    PlacingCard,
    Finished
}
