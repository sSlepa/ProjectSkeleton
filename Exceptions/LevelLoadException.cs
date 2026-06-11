namespace TheAdventure.Exceptions;

public class LevelLoadException : Exception
{
    public LevelLoadException(string message) : base(message) { }
    public LevelLoadException(string message, Exception inner) : base(message, inner) { }
}
