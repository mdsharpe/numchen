# Numchen

## Roadmap

Building in vertical slices, each delivering something runnable:

1. ~~Domain model + tests~~ (done)
2. SignalR hub + game flow — create/join/start game, draw cards, place cards, move to destination piles (server-authoritative)
3. Basic Vue UI — connect to the hub, show the board, play the game
4. Polish — visuals, animations, error handling, edge cases

## Code style

- Methods should be named beginning with a verb (e.g. `GetCanMoveToDestination` not `CanMoveToDestination`). Properties like `IsComplete` are fine.
- Each C# attribute should be on its own line, not combined (e.g. `[Theory]` and `[AutoData]` on separate lines, not `[Theory, AutoData]`).
