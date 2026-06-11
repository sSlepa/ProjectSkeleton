using Silk.NET.Maths;

namespace TheAdventure.Models;

public class Explosion : GameObject
{
    public const double DurationSeconds = 0.55;

    private readonly DateTimeOffset _spawnTime;
    public IReadOnlyList<(int X, int Y)> Cells { get; }

    public Explosion(IReadOnlyList<(int X, int Y)> cells)
    {
        Cells = cells;
        _spawnTime = DateTimeOffset.Now;
    }

    public bool IsExpired => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= DurationSeconds;

    public double Progress =>
        Math.Clamp((DateTimeOffset.Now - _spawnTime).TotalSeconds / DurationSeconds, 0.0, 1.0);

    public bool Covers(int gx, int gy)
    {
        foreach (var c in Cells)
        {
            if (c.X == gx && c.Y == gy) return true;
        }
        return false;
    }

    public void Render(GameRenderer renderer)
    {
        // Two-layer flash: an outer orange and an inner yellow fading out.
        double t = Progress;
        byte alphaOuter = (byte)(255 * (1.0 - t));
        byte alphaInner = (byte)(255 * Math.Max(0.0, 1.0 - t * 1.4));

        foreach (var (gx, gy) in Cells)
        {
            var (wx, wy) = GameMap.GridToWorld(gx, gy);
            renderer.FillRect(255, 120, 0, alphaOuter,
                new Rectangle<int>(wx + 2, wy + 2, GameMap.TileSize - 4, GameMap.TileSize - 4));
            renderer.FillRect(255, 235, 80, alphaInner,
                new Rectangle<int>(wx + 8, wy + 8, GameMap.TileSize - 16, GameMap.TileSize - 16));
        }
    }
}
