# Numchen

## Roadmap

Building in vertical slices, each delivering something runnable:

1. ~~Domain model + tests~~ (done)
2. ~~SignalR hub + game flow~~ — create/join/start game, draw cards, place cards, move to destination piles (server-authoritative) (done)
3. ~~Basic Vue UI~~ — connect to the hub, show the board, play the game (done)
4. Polish
   - Better card styling, layout improvements, color/typography
   - Card animations (placement, moves to destination, draw)
   - Surface hub errors to the user (failures are mostly silent), connection loss feedback
   - Edge cases: player disconnect mid-game, preventing double-clicks, clearing sessionStorage on game finish
   - Multiplayer visibility: leaderboard, surfacing other players' progress, final outcome (who won)
   - Responsive UI for smaller screens
   - Time limit before forcing the next card draw

## Code style

- Methods should be named beginning with a verb (e.g. `GetCanMoveToDestination` not `CanMoveToDestination`). Properties like `IsComplete` are fine.
- Each C# attribute should be on its own line, not combined (e.g. `[Theory]` and `[AutoData]` on separate lines, not `[Theory, AutoData]`).
- All code blocks must use curly braces, including single-line `if` statements.
