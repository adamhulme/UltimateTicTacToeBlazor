# Ultimate Tic Tac Toe (Blazor)

Ultimate Tic Tac Toe implemented in Blazor Server (.NET 8). Play head‑to‑head locally or versus a configurable bot with multiple time controls and difficulty levels.

## Features
- Interactive 3×3 meta‑board of 3×3 small boards (full Ultimate Tic Tac Toe rules)
- Game modes: Unlimited, Blitz/Rapid per‑move timers, Chess clock (with increments), Custom timer
- Single‑player vs Bot: choose side (`X`/`O`), difficulty (`Easy`/`Medium`/`Hard`)
- Score tracking, “New Game” rotation of starting player, and clear visual state for each small board

## Getting Started
Requirements: .NET 8 SDK.

Run locally:
```pwsh
dotnet build
dotnet run
```
Open the URL printed in the terminal (e.g., `http://localhost:5190`).

## How To Play
- A move is placed inside a small board. The cell coordinate you play determines the next board the opponent must play in.
- If the directed board is already complete (win/draw), the next player may move in any active board.
- Win the meta‑board by winning three small boards in a line.

## Single‑Player Bot
- Enable “Play vs Bot” on the setup screen and choose the bot’s side (`X` or `O`).
- Choose difficulty:
  - Easy: lightweight heuristics (finish local wins, block immediate threats, prefer center → corners → sides).
  - Medium/Hard: minimax with alpha–beta pruning, using a combined positional and tactical evaluation.
- The bot observes “must play in next board” and includes a short “thinking” delay before each move.

## Bot Strategy: Minimax In Practice
The stronger bot (`AdvancedBot`) evaluates moves by simulating future positions using minimax search with alpha–beta pruning.

### What the Bot Optimizes
The evaluation blends local small‑board tactics with meta‑board progress:
- Local threats and defenses: lines with two marks and an empty cell are scored highly; contested lines are neutral; immediate opponent threats are penalized.
- Positional value: central squares in a small board are favored, followed by corners, then edges.
- Meta‑board potential: completed small boards are treated as markers on a 3×3 meta grid. Lines trending toward a meta win are rewarded; opponent meta progress is penalized.
- Routing awareness: moves are adjusted based on where they send the opponent next. Sending the opponent to the meta‑center is penalized; routing to boards with limited opportunity reduces the opponent’s initiative. Routing to a completed board (free move anywhere) is penalized.

### Why Alpha–Beta Matters
Minimax explores move trees up to a fixed depth (Medium: 2 plies, Hard: 3 plies). Alpha–beta pruning discards branches that cannot influence the final choice, significantly reducing the number of simulated positions without changing the result of the search.

### Practical Consequences On Playstyle
- Finishes local wins and blocks immediate losses reliably in the targeted small board.
- Avoids gifting initiative on the meta‑center unless there is compensating tactical or meta benefit.
- Prefers local moves that route the opponent into weak or nearly closed boards.
- When free to choose any board, tends to play in meta‑center or meta corners that align with building meta lines—balanced against routing penalties.
- Depth is intentionally shallow to keep the game responsive; it captures short tactical sequences but won’t always see long traps.

## Configuration
Bot thinking delay: configured in `Components/Pages/Home.razor` (`ThinkingDelayMs`).
Difficulty mapping:
- Easy → `SimpleBot`
- Medium → `AdvancedBot(depth: 2)`
- Hard → `AdvancedBot(depth: 3)`

## Project Structure
```
Components/
  Pages/Home.razor         // UI, timers, and game loop (bot integration)
Models/                    // Core game types (cells, small/meta boards, states)
AI/
  IBot.cs                  // Bot interface
  SimpleBot.cs             // Heuristic bot
  AdvancedBot.cs           // Minimax bot with alpha–beta and routing
```

## Notes
- The rules logic lives in `Models/SmallBoard.cs` and `Models/UltimateBoard.cs`.
- The bot never bypasses rule constraints: it only chooses legal moves given the current “next board” rule.
- The UI disables clicks during the bot’s turn and uses a short delay to make bot play feel natural.

## Future Ideas
- Adjustable search depth and evaluation weights via settings
- Train bots using Alpha Zero algorithm
- Analytics overlay to visualize meta pressure and routing impacts