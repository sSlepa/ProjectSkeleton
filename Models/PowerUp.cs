using Silk.NET.Maths;

namespace TheAdventure.Models;

public enum PowerUpKind
{
    ExtraBomb,
    ExtraRange
}

public class PowerUp : GameObject
{
    public int GridX { get; }
    public int GridY { get; }
    public PowerUpKind Kind { get; }
    public bool Collected { get; set; }

    public PowerUp(int gridX, int gridY, PowerUpKind kind)
    {
        GridX = gridX;
        GridY = gridY;
        Kind = kind;
    }

    public void Render(GameRenderer renderer)
    {
        var (wx, wy) = GameMap.GridToWorld(GridX, GridY);
        var pad = 6;
        var rect = new Rectangle<int>(wx + pad, wy + pad, GameMap.TileSize - pad * 2, GameMap.TileSize - pad * 2);

        // Pulse alpha so power-ups stand out from soft-wall debris.
        double pulse = 0.6 + 0.4 * Math.Sin(DateTimeOffset.Now.Ticks / 2_000_000.0);
        byte alpha = (byte)(180 + 75 * pulse);

        switch (Kind)
        {
            case PowerUpKind.ExtraBomb:
                renderer.FillRect(40, 40, 40, alpha, rect);
                renderer.FillRect(255, 80, 0, 255,
                    new Rectangle<int>(wx + GameMap.TileSize / 2 - 2, wy + pad - 2, 4, 4));
                break;
            case PowerUpKind.ExtraRange:
                renderer.FillRect(255, 70, 70, alpha, rect);
                renderer.FillRect(255, 235, 80, 255,
                    new Rectangle<int>(wx + GameMap.TileSize / 2 - 1, wy + pad + 2, 3, GameMap.TileSize - pad * 2 - 4));
                renderer.FillRect(255, 235, 80, 255,
                    new Rectangle<int>(wx + pad + 2, wy + GameMap.TileSize / 2 - 1, GameMap.TileSize - pad * 2 - 4, 3));
                break;
        }
    }
}
