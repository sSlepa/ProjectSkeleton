namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject, IDamageable
{
    private const double Speed = 110.0;
    public const int HitboxSize = 22;

    public bool IsAlive { get; private set; } = true;
    public int MaxBombs { get; private set; } = 1;
    public int ExplosionRange { get; private set; } = 1;
    public int ActiveBombs { get; set; }

    private double _px;
    private double _py;
    private readonly HashSet<int> _ignoredBombs = new();

    public (int X, int Y) GridPosition => GameMap.WorldToGrid(Position.X, Position.Y);

    public PlayerObject(GameRenderer renderer, int spawnGridX, int spawnGridY)
        : base(BuildSpriteSheet(renderer), GameMap.GridCenterToWorld(spawnGridX, spawnGridY))
    {
        var (sx, sy) = GameMap.GridCenterToWorld(spawnGridX, spawnGridY);
        _px = sx;
        _py = sy;
    }

    public void IncreaseBombs() => MaxBombs = Math.Min(MaxBombs + 1, 8);
    public void IncreaseRange() => ExplosionRange = Math.Min(ExplosionRange + 1, 8);

    public void IgnoreBomb(int bombId) => _ignoredBombs.Add(bombId);

    public void Kill() => IsAlive = false;

    public void Update(double up, double down, double left, double right, int dtMs,
        GameMap map, IReadOnlyList<Bomb> bombs)
    {
        if (!IsAlive) return;

        double dx = (right - left) * Speed * dtMs / 1000.0;
        double dy = (down - up) * Speed * dtMs / 1000.0;

        if (dx != 0)
        {
            double newPx = _px + dx;
            if (!Blocked(newPx, _py, map, bombs))
            {
                _px = newPx;
            }
        }

        if (dy != 0)
        {
            double newPy = _py + dy;
            if (!Blocked(_px, newPy, map, bombs))
            {
                _py = newPy;
            }
        }

        Position = ((int)Math.Round(_px), (int)Math.Round(_py));

        _ignoredBombs.RemoveWhere(id =>
        {
            var bomb = bombs.FirstOrDefault(b => b.Id == id);
            if (bomb == null) return true;
            return !HitboxIntersectsCell(_px, _py, bomb.GridX, bomb.GridY);
        });
    }

    private bool Blocked(double px, double py, GameMap map, IReadOnlyList<Bomb> bombs)
    {
        var (gx0, gy0, gx1, gy1) = CoveredCells(px, py);
        for (int gy = gy0; gy <= gy1; gy++)
        {
            for (int gx = gx0; gx <= gx1; gx++)
            {
                if (!map.IsWalkable(gx, gy)) return true;
            }
        }

        foreach (var bomb in bombs)
        {
            if (_ignoredBombs.Contains(bomb.Id)) continue;
            if (gx0 <= bomb.GridX && bomb.GridX <= gx1 &&
                gy0 <= bomb.GridY && bomb.GridY <= gy1)
            {
                return true;
            }
        }

        return false;
    }

    private static (int Gx0, int Gy0, int Gx1, int Gy1) CoveredCells(double px, double py)
    {
        int hx = (int)Math.Round(px) - HitboxSize / 2;
        int hy = (int)Math.Round(py) - HitboxSize / 2;
        var (gx0, gy0) = GameMap.WorldToGrid(hx, hy);
        var (gx1, gy1) = GameMap.WorldToGrid(hx + HitboxSize - 1, hy + HitboxSize - 1);
        return (gx0, gy0, gx1, gy1);
    }

    private static bool HitboxIntersectsCell(double px, double py, int gx, int gy)
    {
        var (gx0, gy0, gx1, gy1) = CoveredCells(px, py);
        return gx0 <= gx && gx <= gx1 && gy0 <= gy && gy <= gy1;
    }

    private static SpriteSheet BuildSpriteSheet(GameRenderer renderer)
    {
        return new SpriteSheet(renderer, Path.Combine("Assets", "player.png"), 1, 1, 48, 48, (24, 24));
    }
}
