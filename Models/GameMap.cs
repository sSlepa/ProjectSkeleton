namespace TheAdventure.Models;

public class GameMap
{
    public const int TileSize = 32;
    public const int Width = 17;
    public const int Height = 11;
    public const int HudHeight = 48;

    public const int OffsetX = (640 - Width * TileSize) / 2;
    public const int OffsetY = HudHeight + (400 - HudHeight - Height * TileSize) / 2;

    private readonly TileType[,] _tiles = new TileType[Width, Height];
    private readonly Random _random;

    public GameMap(Random random, double softWallChance = 0.55)
    {
        _random = random;
        Generate(softWallChance);
    }

    public TileType this[int x, int y]
    {
        get
        {
            if (!InBounds(x, y)) return TileType.HardWall;
            return _tiles[x, y];
        }
        set
        {
            if (InBounds(x, y))
            {
                _tiles[x, y] = value;
            }
        }
    }

    public static bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public bool IsWalkable(int x, int y) => InBounds(x, y) && _tiles[x, y] == TileType.Empty;

    public IEnumerable<(int X, int Y)> EmptyCells()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_tiles[x, y] == TileType.Empty)
                {
                    yield return (x, y);
                }
            }
        }
    }

    public static (int X, int Y) GridToWorld(int gx, int gy) =>
        (OffsetX + gx * TileSize, OffsetY + gy * TileSize);

    public static (int X, int Y) GridCenterToWorld(int gx, int gy) =>
        (OffsetX + gx * TileSize + TileSize / 2, OffsetY + gy * TileSize + TileSize / 2);

    public static (int Gx, int Gy) WorldToGrid(int worldX, int worldY) =>
        ((worldX - OffsetX) / TileSize, (worldY - OffsetY) / TileSize);

    private void Generate(double softWallChance)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                bool isBorder = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                bool isPillar = x % 2 == 0 && y % 2 == 0;
                _tiles[x, y] = (isBorder || isPillar) ? TileType.HardWall : TileType.Empty;
            }
        }

        var safeSpawn = new HashSet<(int, int)>
        {
            (1, 1), (2, 1), (1, 2),
            (Width - 2, Height - 2), (Width - 3, Height - 2), (Width - 2, Height - 3),
            (1, Height - 2), (2, Height - 2), (1, Height - 3),
            (Width - 2, 1), (Width - 3, 1), (Width - 2, 2),
        };

        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                if (_tiles[x, y] != TileType.Empty) continue;
                if (safeSpawn.Contains((x, y))) continue;
                if (_random.NextDouble() < softWallChance)
                {
                    _tiles[x, y] = TileType.SoftWall;
                }
            }
        }
    }
}
