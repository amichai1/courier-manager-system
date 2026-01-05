namespace BlApi;

using BIApi;
using BO;
using Helpers;
using System.Collections.Generic;

/// <summary>
/// Defines the service contract for managing Order entities.
/// </summary>
public interface IBIOrder : BIApi.IObservable
{
    // CRUD Operations
    void Create(BO.Order order);
    BO.Order Read(int id);
    IEnumerable<BO.Order> ReadAll(Func<BO.Order, bool>? filter = null);
    void Update(BO.Order order);
    void Delete(int id);

    // List Operations
    /// <summary>
    /// Gets all orders as a lightweight list for display purposes.
    /// </summary>
    IEnumerable<BO.OrderInList> GetOrderList();

    // Specific Operations
    void AssociateCourierToOrder(int orderId, int courierId);
    void PickUpOrder(int orderId);
    void DeliverOrder(int orderId);

    /// <summary>
    /// Cancels an order. If the order is in progress, sends email to courier.
    /// </summary>
    /// <param name="orderId">The ID of the order to cancel</param>
    void CancelOrder(int orderId);

    /// <summary>
    /// Get available orders for a specific courier, by distance from courier's location
    /// </summary>
    IEnumerable<BO.Order> GetAvailableOrdersForCourier(int courierId);

    /// <summary>
    /// Gets a summary of order counts grouped by OrderStatus and ScheduleStatus.
    /// Used for the main dashboard display.
    /// </summary>
    BO.OrderStatusSummary GetOrderStatusSummary();

    /// <summary>
    /// Gets the delivery history for a specific order.
    /// </summary>
    IEnumerable<BO.DeliveryPerOrderInList> GetDeliveryHistoryForOrder(int orderId);
}
