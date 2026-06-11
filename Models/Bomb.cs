using Silk.NET.Maths;

namespace TheAdventure.Models;

public class Bomb : GameObject
{
    public const double FuseSeconds = 2.5;

    public int GridX { get; }
    public int GridY { get; }
    public int Range { get; }

    private readonly DateTimeOffset _spawnTime;

    public Bomb(int gridX, int gridY, int range)
    {
        GridX = gridX;
        GridY = gridY;
        Range = range;
        _spawnTime = DateTimeOffset.Now;
    }

    public bool ShouldExplode => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= FuseSeconds;

    public double FuseProgress =>
        Math.Clamp((DateTimeOffset.Now - _spawnTime).TotalSeconds / FuseSeconds, 0.0, 1.0);

    public void Render(GameRenderer renderer)
    {
        var (wx, wy) = GameMap.GridToWorld(GridX, GridY);
        // Body: dark square that pulses smaller as the fuse counts down.
        double pulse = 0.5 + 0.5 * Math.Sin((DateTimeOffset.Now - _spawnTime).TotalSeconds * 8.0);
        int shrink = (int)(pulse * 4);
        int margin = 4 + shrink;
        renderer.FillRect(20, 20, 20, 255,
            new Rectangle<int>(wx + margin, wy + margin,
                GameMap.TileSize - margin * 2, GameMap.TileSize - margin * 2));

        // Fuse spark on top, brighter as it gets closer to detonation.
        byte sparkRed = (byte)(180 + 75 * FuseProgress);
        renderer.FillRect(sparkRed, 80, 0, 255,
            new Rectangle<int>(wx + GameMap.TileSize / 2 - 2, wy + 2, 4, 4));
    }
}
