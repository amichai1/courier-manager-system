namespace BlApi;

using BIApi;
using BO;
using Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    #region Stage 7 - Async Network Operations

    /// <summary>
    /// Creates an order with async geocoding of the address.
    /// Returns geocoding status for UI feedback.
    /// </summary>
    /// <param name="order">The order to create</param>
    /// <returns>Tuple of (success, errorMessage, geocodeStatus)</returns>
    Task<(bool success, string? errorMessage, int geocodeStatus)> CreateOrderAsync(BO.Order order);

    /// <summary>
    /// Updates an order with async geocoding if address changed.
    /// </summary>
    /// <param name="order">The order to update</param>
    /// <param name="originalAddress">The original address before update</param>
    /// <returns>Tuple of (success, errorMessage, geocodeStatus)</returns>
    Task<(bool success, string? errorMessage, int geocodeStatus)> UpdateOrderAsync(BO.Order order, string? originalAddress);

    /// <summary>
    /// Gets available orders for a courier with actual route distances (not air distance).
    /// Uses OSRM API for real driving/walking distance calculation.
    /// </summary>
    /// <param name="courierId">The courier ID</param>
    /// <returns>Orders sorted by route distance</returns>
    Task<IEnumerable<BO.Order>> GetAvailableOrdersWithDistanceAsync(int courierId);

    /// <summary>
    /// Gets order list with route distances calculated asynchronously.
    /// </summary>
    /// <returns>Order list with updated distances</returns>
    Task<IEnumerable<BO.OrderInList>> GetOrderListWithDistancesAsync();

    #endregion Stage 7
}
