namespace TheAdventure.Models;

public enum Direction
{
    None,
    Up,
    Down,
    Left,
    Right
}

public static class DirectionExtensions
{
    public static (int Dx, int Dy) ToOffset(this Direction direction) => direction switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        Direction.Right => (1, 0),
        _ => (0, 0)
    };

    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => Direction.None
    };
}
