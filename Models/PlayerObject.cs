namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int Speed = 128;

    public int X => Position.X;
    public int Y => Position.Y;

    public PlayerObject(GameRenderer renderer)
        : base(BuildSpriteSheet(renderer), (100, 100))
    {
    }

    public void UpdatePosition(double up, double down, double left, double right, int time)
    {
        var pixelsToMove = Speed * (time / 1000.0);

        var (x, y) = Position;
        y -= (int)(pixelsToMove * up);
        y += (int)(pixelsToMove * down);
        x -= (int)(pixelsToMove * left);
        x += (int)(pixelsToMove * right);

        Position = (x, y);
    }

    private static SpriteSheet BuildSpriteSheet(GameRenderer renderer)
    {
        return new SpriteSheet(renderer, Path.Combine("Assets", "player.png"), 1, 1, 48, 48, (-24, 42));
    }
}
