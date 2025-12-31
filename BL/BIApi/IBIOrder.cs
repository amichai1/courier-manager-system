namespace BlApi;

using BIApi; // Add this using statement
using BO;
using Helpers;
using System.Collections.Generic;

/// <summary>
/// Defines the service contract for managing Order entities.
/// </summary>
public interface IBIOrder : BIApi.IObservable // Stage 5 - Extend IObservable (use fully qualified name)
{
    // CRUD Operations
    void Create(BO.Order order);
    BO.Order Read(int id);
    IEnumerable<BO.Order> ReadAll(Func<BO.Order, bool>? filter = null);
    void Update(BO.Order order);
    void Delete(int id);

    // Specific Operations
    void AssociateCourierToOrder(int orderId, int courierId);
    void PickUpOrder(int orderId);
    void DeliverOrder(int orderId);
    /// <summary>
    /// Get available orders for a specific courier, by distance from courier's location
    /// </summary>
    IEnumerable<BO.Order> GetAvailableOrdersForCourier(int courierId);
}
