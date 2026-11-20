namespace Centesimo.Application;

/// <summary>Told by the store that a write is in the database for good, after the data is visible to every
/// connection. It runs inside the writer's call, so an implementation must return quickly and may be called more
/// than once for one logical write.</summary>
public interface IStoreChangeListener
{
    void StoreChanged();
}
