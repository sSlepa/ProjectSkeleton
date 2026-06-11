namespace TheAdventure.Models;

public interface IDamageable
{
    bool IsAlive { get; }
    (int X, int Y) GridPosition { get; }
    void Kill();
}
