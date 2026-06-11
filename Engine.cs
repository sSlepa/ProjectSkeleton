using Silk.NET.Maths;
using TheAdventure.Exceptions;
using TheAdventure.Game;
using TheAdventure.Models;

namespace TheAdventure;

// AI-generated
public class Engine
{
    private const int InitialEnemies = 3;
    private const int MinEnemySpawnDistance = 6;
    private const double PowerUpDropChance = 0.30;
    private const int ScorePerWall = 10;
    private const int ScorePerEnemy = 100;
    private const int ScorePerPowerUp = 25;
    private const int ScoreWinBonus = 500;

    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly Random _random = new();
    private readonly HighScoreStore _scoreStore = new();

    private readonly List<Bomb> _bombs = new();
    private readonly List<Explosion> _explosions = new();
    private readonly List<Enemy> _enemies = new();
    private readonly List<PowerUp> _powerUps = new();

    private readonly int[] _grassTextureIds = new int[4];
    private int[,] _grassPattern = new int[GameMap.Width, GameMap.Height];

    private GameMap _map = null!;
    private PlayerObject _player = null!;
    private HighScoreData _scoreData = new();

    private GameState _state = GameState.Playing;
    private int _score;
    private int _enemiesKilled;
    private int _wallsDestroyed;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;
    }

    public async Task SetupWorldAsync()
    {
        try
        {
            for (int i = 0; i < 4; i++)
            {
                _grassTextureIds[i] = _renderer.LoadTexture(
                    Path.Combine("Assets", $"grass_{(i + 1):D3}.png"), out _);
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new LevelLoadException("Failed to load floor textures from Assets/.", ex);
        }

        _scoreData = await _scoreStore.LoadAsync();

        var (width, height) = _renderer.WindowSize;
        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, width, height));

        StartNewGame();
    }

    public async Task ShutdownAsync()
    {
        await _scoreStore.SaveAsync(_scoreData);
    }

    public void ProcessFrame()
    {
        var now = DateTimeOffset.Now;
        // Clamp the step so a frame hitch (window drag, GC pause, slow first frame)
        // cannot move entities far enough to tunnel through a wall in a single update.
        int dtMs = Math.Min((int)(now - _lastUpdate).TotalMilliseconds, 50);
        _lastUpdate = now;

        if (_state == GameState.Playing)
        {
            UpdatePlaying(dtMs);
        }
        else
        {
            // On game over, allow restart with Enter.
            if (_input.WasKeyJustPressed(KeyCode.Return))
            {
                StartNewGame();
            }
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        RenderFloor();
        RenderWalls();
        RenderPowerUps();
        RenderBombs();
        RenderEnemies();
        RenderExplosions();
        _player.Render(_renderer);
        RenderHud();
        RenderOverlay();

        _renderer.PresentFrame();
    }

    private void StartNewGame()
    {
        _bombs.Clear();
        _explosions.Clear();
        _enemies.Clear();
        _powerUps.Clear();
        _score = 0;
        _enemiesKilled = 0;
        _wallsDestroyed = 0;
        _state = GameState.Playing;

        _map = new GameMap(_random);

        for (int y = 0; y < GameMap.Height; y++)
        {
            for (int x = 0; x < GameMap.Width; x++)
            {
                _grassPattern[x, y] = _random.Next(4);
            }
        }

        _player = new PlayerObject(_renderer, 1, 1);

        var enemySpawns = _map.EmptyCells()
            .Where(c => Math.Abs(c.X - 1) + Math.Abs(c.Y - 1) >= MinEnemySpawnDistance)
            .OrderBy(_ => _random.Next())
            .Take(InitialEnemies)
            .ToList();

        foreach (var cell in enemySpawns)
        {
            _enemies.Add(new Enemy(cell.X, cell.Y, _random));
        }
    }

    private void UpdatePlaying(int dtMs)
    {
        // Player movement.
        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        _player.Update(up, down, left, right, dtMs, _map, _bombs);

        // Place a bomb on Space.
        if (_input.WasKeyJustPressed(KeyCode.Space))
        {
            TryPlaceBomb();
        }

        // Bombs.
        var dueBombs = _bombs.Where(b => b.ShouldExplode).ToList();
        foreach (var bomb in dueBombs)
        {
            // A chained detonation may have already removed this bomb earlier in the loop.
            if (_bombs.Contains(bomb))
            {
                DetonateChain(bomb);
            }
        }

        // Enemies.
        foreach (var enemy in _enemies)
        {
            enemy.Update(dtMs, _map, _bombs);
        }

        // Enemies that walk into a still-active explosion die too, like the player does.
        foreach (var enemy in _enemies)
        {
            if (TryKillInExplosion(enemy))
            {
                _enemiesKilled++;
                _score += ScorePerEnemy;
            }
        }

        // Pickups.
        var (pgx, pgy) = _player.GridPosition;
        foreach (var p in _powerUps.Where(p => !p.Collected && p.GridX == pgx && p.GridY == pgy))
        {
            ApplyPowerUp(p);
            p.Collected = true;
        }
        _powerUps.RemoveAll(p => p.Collected);

        // Player hit by an active explosion.
        TryKillInExplosion(_player);

        // Player touched by an enemy.
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive) continue;
            if (enemy.TouchesPlayerHitbox(_player.Position.X, _player.Position.Y))
            {
                _player.Kill();
                break;
            }
        }

        // Expire explosions.
        _explosions.RemoveAll(e => e.IsExpired);

        // End-of-game checks.
        if (!_player.IsAlive)
        {
            EndGame(GameState.Lost);
        }
        else if (_enemies.All(e => !e.IsAlive))
        {
            _score += ScoreWinBonus;
            EndGame(GameState.Won);
        }
    }

    private void TryPlaceBomb()
    {
        if (_player.ActiveBombs >= _player.MaxBombs) return;

        var (gx, gy) = _player.GridPosition;
        if (!GameMap.InBounds(gx, gy)) return;
        if (_map[gx, gy] != TileType.Empty) return;
        if (_bombs.Any(b => b.GridX == gx && b.GridY == gy)) return;

        var bomb = new Bomb(gx, gy, _player.ExplosionRange);
        _bombs.Add(bomb);
        _player.ActiveBombs++;
        _player.IgnoreBomb(bomb.Id);
    }

    private void DetonateChain(Bomb starting)
    {
        var queue = new Queue<Bomb>();
        queue.Enqueue(starting);
        var explodedIds = new HashSet<int>();
        var allCells = new List<(int X, int Y)>();

        while (queue.Count > 0)
        {
            var bomb = queue.Dequeue();
            if (!explodedIds.Add(bomb.Id)) continue;

            var cells = ComputeExplosionCells(bomb);
            foreach (var cell in cells)
            {
                if (!allCells.Contains(cell))
                {
                    allCells.Add(cell);
                }

                foreach (var other in _bombs)
                {
                    if (other.Id == bomb.Id) continue;
                    if (explodedIds.Contains(other.Id)) continue;
                    if (other.GridX == cell.X && other.GridY == cell.Y)
                    {
                        queue.Enqueue(other);
                    }
                }
            }
        }

        _bombs.RemoveAll(b => explodedIds.Contains(b.Id));
        _player.ActiveBombs = Math.Max(0, _player.ActiveBombs - explodedIds.Count);

        ApplyExplosionEffects(allCells);
        _explosions.Add(new Explosion(allCells));
    }

    private List<(int X, int Y)> ComputeExplosionCells(Bomb bomb)
    {
        var cells = new List<(int X, int Y)> { (bomb.GridX, bomb.GridY) };
        Direction[] dirs = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        foreach (var dir in dirs)
        {
            var (dx, dy) = dir.ToOffset();
            for (int i = 1; i <= bomb.Range; i++)
            {
                int x = bomb.GridX + dx * i;
                int y = bomb.GridY + dy * i;
                if (!GameMap.InBounds(x, y)) break;
                var tile = _map[x, y];
                if (tile == TileType.HardWall) break;
                cells.Add((x, y));
                if (tile == TileType.SoftWall) break;
            }
        }
        return cells;
    }

    private void ApplyExplosionEffects(List<(int X, int Y)> cells)
    {
        // New drops are collected separately and added after the destroy pass below,
        // otherwise a power-up uncovered by this blast would be destroyed by it instantly.
        var dropped = new List<PowerUp>();

        foreach (var (gx, gy) in cells)
        {
            if (_map[gx, gy] == TileType.SoftWall)
            {
                _map[gx, gy] = TileType.Empty;
                _wallsDestroyed++;
                _score += ScorePerWall;

                if (_random.NextDouble() < PowerUpDropChance)
                {
                    var kind = _random.Next(2) == 0 ? PowerUpKind.ExtraBomb : PowerUpKind.ExtraRange;
                    dropped.Add(new PowerUp(gx, gy, kind));
                }
            }

            // Power-ups already on the ground inside the blast are destroyed too.
            foreach (var p in _powerUps.Where(p => p.GridX == gx && p.GridY == gy))
            {
                p.Collected = true;
            }

            foreach (var enemy in _enemies)
            {
                if (!enemy.IsAlive) continue;
                if (enemy.GridPosition == (gx, gy))
                {
                    enemy.Kill();
                    _enemiesKilled++;
                    _score += ScorePerEnemy;
                }
            }
        }

        _powerUps.RemoveAll(p => p.Collected);
        _powerUps.AddRange(dropped);
    }

    private void ApplyPowerUp(PowerUp p)
    {
        switch (p.Kind)
        {
            case PowerUpKind.ExtraBomb:
                _player.IncreaseBombs();
                _score += ScorePerPowerUp;
                break;
            case PowerUpKind.ExtraRange:
                _player.IncreaseRange();
                _score += ScorePerPowerUp;
                break;
        }
    }

    // Shared death-by-explosion rule for anything damageable (player and enemies):
    // you die if your centre cell is covered by a still-active blast.
    private bool TryKillInExplosion(IDamageable target)
    {
        if (!target.IsAlive) return false;

        var (gx, gy) = target.GridPosition;
        if (!_explosions.Any(e => e.Covers(gx, gy))) return false;

        target.Kill();
        return true;
    }

    private void EndGame(GameState newState)
    {
        if (_state != GameState.Playing) return;

        _state = newState;
        _scoreData.TotalGames++;
        if (newState == GameState.Won) _scoreData.TotalWins++;
        if (_score > _scoreData.HighScore) _scoreData.HighScore = _score;

        // Fire-and-forget save: we already keep an in-memory copy and Shutdown awaits a final save.
        _ = _scoreStore.SaveAsync(_scoreData);
    }

    private void RenderFloor()
    {
        for (int y = 0; y < GameMap.Height; y++)
        {
            for (int x = 0; x < GameMap.Width; x++)
            {
                var (wx, wy) = GameMap.GridToWorld(x, y);
                var src = new Rectangle<int>(0, 0, 16, 16);
                var dst = new Rectangle<int>(wx, wy, GameMap.TileSize, GameMap.TileSize);
                _renderer.RenderTextureRaw(_grassTextureIds[_grassPattern[x, y]], src, dst);
            }
        }
    }

    private void RenderWalls()
    {
        for (int y = 0; y < GameMap.Height; y++)
        {
            for (int x = 0; x < GameMap.Width; x++)
            {
                var tile = _map[x, y];
                if (tile == TileType.Empty) continue;

                var (wx, wy) = GameMap.GridToWorld(x, y);
                var rect = new Rectangle<int>(wx, wy, GameMap.TileSize, GameMap.TileSize);
                if (tile == TileType.HardWall)
                {
                    _renderer.FillRect(80, 80, 90, 255, rect);
                    _renderer.FillRect(110, 110, 120, 255,
                        new Rectangle<int>(wx + 2, wy + 2, GameMap.TileSize - 4, GameMap.TileSize - 4));
                    _renderer.DrawRect(40, 40, 50, 255, rect);
                }
                else if (tile == TileType.SoftWall)
                {
                    _renderer.FillRect(140, 90, 50, 255, rect);
                    _renderer.FillRect(170, 110, 60, 255,
                        new Rectangle<int>(wx + 2, wy + 2, GameMap.TileSize - 4, GameMap.TileSize - 4));
                    _renderer.DrawRect(70, 40, 20, 255, rect);
                }
            }
        }
    }

    private void RenderPowerUps()
    {
        foreach (var p in _powerUps)
        {
            p.Render(_renderer);
        }
    }

    private void RenderBombs()
    {
        foreach (var b in _bombs)
        {
            b.Render(_renderer);
        }
    }

    private void RenderEnemies()
    {
        foreach (var e in _enemies)
        {
            e.Render(_renderer);
        }
    }

    private void RenderExplosions()
    {
        foreach (var ex in _explosions)
        {
            ex.Render(_renderer);
        }
    }

    private void RenderHud()
    {
        var (winW, _) = _renderer.WindowSize;
        _renderer.FillRect(15, 15, 25, 255, new Rectangle<int>(0, 0, winW, GameMap.HudHeight));
        _renderer.FillRect(60, 60, 80, 255, new Rectangle<int>(0, GameMap.HudHeight - 2, winW, 2));

        const int scale = 2;
        int y = (GameMap.HudHeight - PixelFont.Height(scale)) / 2;
        int x = 8;

        x = DrawLabel($"SCORE:{_score}", x, y, scale, 240, 240, 240);
        x = DrawLabel($"BEST:{_scoreData.HighScore}", x + 12, y, scale, 240, 220, 100);
        x = DrawLabel($"BOMBS:{_player.MaxBombs}", x + 12, y, scale, 200, 200, 255);
        x = DrawLabel($"RANGE:{_player.ExplosionRange}", x + 12, y, scale, 200, 255, 200);
        DrawLabel($"FOES:{_enemies.Count(e => e.IsAlive)}", x + 12, y, scale, 255, 180, 180);
    }

    private int DrawLabel(string text, int x, int y, int scale, byte r, byte g, byte b)
    {
        PixelFont.Draw(_renderer, text, x, y, scale, r, g, b);
        return x + PixelFont.Measure(text, scale);
    }

    private void DrawCentered(string text, int areaWidth, int y, int scale, byte r, byte g, byte b)
    {
        int x = (areaWidth - PixelFont.Measure(text, scale)) / 2;
        PixelFont.Draw(_renderer, text, x, y, scale, r, g, b);
    }

    private void RenderOverlay()
    {
        if (_state == GameState.Playing) return;

        var (winW, winH) = _renderer.WindowSize;
        _renderer.FillRect(0, 0, 0, 180, new Rectangle<int>(0, 0, winW, winH));

        string title = _state == GameState.Won ? "YOU WIN!" : "GAME OVER";
        byte tr = _state == GameState.Won ? (byte)180 : (byte)240;
        byte tg = _state == GameState.Won ? (byte)255 : (byte)80;
        byte tb = _state == GameState.Won ? (byte)180 : (byte)80;

        const int titleScale = 6;
        int titleY = winH / 2 - 60;
        DrawCentered(title, winW, titleY, titleScale, tr, tg, tb);

        const int subScale = 2;
        string sub1 = $"SCORE {_score}   BEST {_scoreData.HighScore}";
        string sub2 = $"FOES {_enemiesKilled}   WALLS {_wallsDestroyed}";
        string sub3 = $"WINS {_scoreData.TotalWins} OF {_scoreData.TotalGames} PLAYED";
        string sub4 = "ENTER TO RESTART  -  ESC TO QUIT";
        int baseY = titleY + PixelFont.Height(titleScale) + 16;
        DrawCentered(sub1, winW, baseY, subScale, 230, 230, 230);
        DrawCentered(sub2, winW, baseY + 18, subScale, 200, 220, 200);
        DrawCentered(sub3, winW, baseY + 36, subScale, 200, 220, 200);
        DrawCentered(sub4, winW, baseY + 58, subScale, 200, 200, 200);
    }
}
// end AI-generated
