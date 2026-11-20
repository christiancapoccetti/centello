namespace Centesimo.Application;

/// <summary>Told after every write that reached the database for good. It runs inside the writer's call on the
/// writer's thread, so it must return at once.</summary>
public interface IStoreChangeListener
{
    void StoreChanged();
}
