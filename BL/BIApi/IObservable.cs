namespace BIApi;

/// <summary>
/// Defines the observer pattern contract for entities in the Business Logic layer.
/// Allows UI components (Observers) to subscribe to changes in logical entities.
/// </summary>
public interface IObservable
{
    /// <summary>
    /// Add an observer method to be notified of changes to the entire list of entities.
    /// </summary>
    /// <param name="listObserver">Observer method to be called when the list changes.</param>
    void AddObserver(Action listObserver);

    /// <summary>
    /// Add an observer method to be notified of changes to a specific entity instance.
    /// </summary>
    /// <param name="id">The unique ID of the entity to observe.</param>
    /// <param name="observer">Observer method to be called when the specific entity changes.</param>
    void AddObserver(int id, Action observer);

    /// <summary>
    /// Remove an observer method from list change notifications.
    /// </summary>
    /// <param name="listObserver">Observer method to be removed.</param>
    void RemoveObserver(Action listObserver);

    /// <summary>
    /// Remove an observer method from specific entity change notifications.
    /// </summary>
    /// <param name="id">The unique ID of the entity being observed.</param>
    /// <param name="observer">Observer method to be removed.</param>
    void RemoveObserver(int id, Action observer);
}
