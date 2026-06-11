using Silk.NET.Maths;

namespace TheAdventure.Models;

public class Enemy : GameObject, IDamageable
{
    private const double Speed = 55.0;
    private const int HitboxSize = 20;

    public bool IsAlive { get; private set; } = true;
    public (int X, int Y) GridPosition => GameMap.WorldToGrid((int)Math.Round(_px), (int)Math.Round(_py));

    private double _px;
    private double _py;
    private Direction _direction;
    private readonly Random _random;

    public Enemy(int spawnGridX, int spawnGridY, Random random)
    {
        var (wx, wy) = GameMap.GridCenterToWorld(spawnGridX, spawnGridY);
        _px = wx;
        _py = wy;
        _random = random;
        _direction = PickRandomDirection();
    }

    public void Kill() => IsAlive = false;

    public void Update(int dtMs, GameMap map, IReadOnlyList<Bomb> bombs)
    {
        if (!IsAlive) return;

        // If aligned with a tile center on the perpendicular axis, consider turning at intersections.
        bool nearCenter = NearTileCenter();
        if (nearCenter)
        {
            // If current direction is blocked at the next cell, pick a new one.
            if (!CanContinue(_direction, map, bombs))
            {
                _direction = PickValidDirection(map, bombs);
            }
            else if (_random.NextDouble() < 0.06)
            {
                // Occasionally pick a new random direction at intersections to make it less predictable.
                var alt = PickValidDirection(map, bombs);
                if (alt != Direction.None) _direction = alt;
            }
        }

        var (dx, dy) = _direction.ToOffset();
        double moveDist = Speed * dtMs / 1000.0;
        double newPx = _px + dx * moveDist;
        double newPy = _py + dy * moveDist;

        if (Blocked(newPx, newPy, map, bombs))
        {
            // Snap to current cell center to avoid getting stuck partway through a wall.
            var (gx, gy) = GameMap.WorldToGrid((int)Math.Round(_px), (int)Math.Round(_py));
            var (cx, cy) = GameMap.GridCenterToWorld(gx, gy);
            _px = cx;
            _py = cy;
            _direction = PickValidDirection(map, bombs);
        }
        else
        {
            _px = newPx;
            _py = newPy;
        }
    }

    public bool TouchesPlayerHitbox(int playerX, int playerY)
    {
        int ex = (int)Math.Round(_px) - HitboxSize / 2;
        int ey = (int)Math.Round(_py) - HitboxSize / 2;
        int phx = playerX - PlayerObject.HitboxSize / 2;
        int phy = playerY - PlayerObject.HitboxSize / 2;
        return ex < phx + PlayerObject.HitboxSize && ex + HitboxSize > phx &&
               ey < phy + PlayerObject.HitboxSize && ey + HitboxSize > phy;
    }

    public void Render(GameRenderer renderer)
    {
        if (!IsAlive) return;
        int x = (int)Math.Round(_px);
        int y = (int)Math.Round(_py);
        var body = new Rectangle<int>(x - HitboxSize / 2, y - HitboxSize / 2, HitboxSize, HitboxSize);
        renderer.FillRect(200, 40, 40, 255, body);
        // Eyes: two small white squares.
        renderer.FillRect(255, 255, 255, 255, new Rectangle<int>(x - 5, y - 4, 3, 3));
        renderer.FillRect(255, 255, 255, 255, new Rectangle<int>(x + 2, y - 4, 3, 3));
    }

    private bool NearTileCenter()
    {
        var (gx, gy) = GameMap.WorldToGrid((int)Math.Round(_px), (int)Math.Round(_py));
        var (cx, cy) = GameMap.GridCenterToWorld(gx, gy);
        return Math.Abs(_px - cx) < 1.5 && Math.Abs(_py - cy) < 1.5;
    }

    private bool CanContinue(Direction dir, GameMap map, IReadOnlyList<Bomb> bombs)
    {
        var (gx, gy) = GameMap.WorldToGrid((int)Math.Round(_px), (int)Math.Round(_py));
        var (dx, dy) = dir.ToOffset();
        int nx = gx + dx;
        int ny = gy + dy;
        if (!map.IsWalkable(nx, ny)) return false;
        foreach (var b in bombs)
        {
            if (b.GridX == nx && b.GridY == ny) return false;
        }
        return true;
    }

    private Direction PickValidDirection(GameMap map, IReadOnlyList<Bomb> bombs)
    {
        var options = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        // Shuffle in place.
        for (int i = options.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }
        Direction fallback = Direction.None;
        foreach (var d in options)
        {
            if (CanContinue(d, map, bombs))
            {
                if (d != _direction.Opposite())
                {
                    return d;
                }
                fallback = d;
            }
        }
        return fallback;
    }

    private Direction PickRandomDirection()
    {
        return (_random.Next(4)) switch
        {
            0 => Direction.Up,
            1 => Direction.Down,
            2 => Direction.Left,
            _ => Direction.Right
        };
    }

    private static bool Blocked(double px, double py, GameMap map, IReadOnlyList<Bomb> bombs)
    {
        int hx = (int)Math.Round(px) - HitboxSize / 2;
        int hy = (int)Math.Round(py) - HitboxSize / 2;
        var (gx0, gy0) = GameMap.WorldToGrid(hx, hy);
        var (gx1, gy1) = GameMap.WorldToGrid(hx + HitboxSize - 1, hy + HitboxSize - 1);
        for (int gy = gy0; gy <= gy1; gy++)
        {
            for (int gx = gx0; gx <= gx1; gx++)
            {
                if (!map.IsWalkable(gx, gy)) return true;
            }
        }
        foreach (var b in bombs)
        {
            if (gx0 <= b.GridX && b.GridX <= gx1 && gy0 <= b.GridY && b.GridY <= gy1)
            {
                return true;
            }
        }
        return false;
    }
}
