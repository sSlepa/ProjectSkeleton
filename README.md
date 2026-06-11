# Bomberman (UVT .NET final assignment)

Top-down grid Bomberman built on top of the
[`dotNETUVT/ProjectSkeleton`](https://github.com/dotNETUVT/ProjectSkeleton) starter.
SDL2 via `Silk.NET.SDL`, .NET 10. Walls, bombs, enemies and explosions are drawn with
`SDL_RenderFillRect`; the floor and the player use the bitmap assets shipped with the
skeleton.

## Build and run

```
dotnet run
```

(from the repository root, on Windows). Requires the .NET 10 SDK. The first run
creates `%LocalAppData%\BombermanSkeleton\scores.json` to persist the high score.

## Controls

| Key | Action |
|-----|--------|
| Arrow keys / WASD | Move |
| Space | Drop a bomb |
| Enter | Restart after win/loss |
| Esc | Quit |

## Rules

- The arena is a 17x11 grid of hard pillars (border + every even/even cell) and
  soft walls (random fill). Bombs explode in a cross with radius `R`, blocked by
  hard walls, destroying a single soft wall in their path.
- Destroyed soft walls drop a random power-up with 30% chance: extra bomb capacity
  (`+B`) or extra explosion range (`+R`).
- Win condition: all enemies dead. Lose condition: the player walks into an
  active explosion or is touched by an enemy.
- Score: 10 per soft wall, 100 per enemy, 25 per power-up, 500 win bonus.
  Best score is persisted across runs.

## Project layout

- `Engine.cs` — main loop (input → update → render).
- `Models/GameMap.cs` — logical grid + Bomberman-style level generation.
- `Models/Bomb.cs`, `Models/Explosion.cs` — bomb / explosion entities.
- `Models/Enemy.cs` — random-walking enemy with intersection turn logic.
- `Models/PlayerObject.cs` — grid-aware player with AABB collision against
  walls and bombs (including the "bomb you just placed is passable" rule).
- `Models/PowerUp.cs` — power-up entity dropped by destroyed soft walls.
- `Game/HighScoreStore.cs` — async JSON load/save of the high-score file.
- `Game/PixelFont.cs` — 3x5 bitmap font drawn via `FillRect` (HUD/overlay).
- `Exceptions/LevelLoadException.cs` — custom exception thrown if the floor
  textures are missing from `Assets/`.

## AI usage

See [`AI_USAGE.md`](AI_USAGE.md) for the full disclosure.
