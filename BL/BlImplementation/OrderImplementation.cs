namespace BL.BIImplementation;

using BlApi;
using BO;
using BL.Helpers;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using global::Helpers;

/// <summary>
/// Implements the IBIOrder service contract, delegating logic to OrderManager.
/// </summary>
internal class OrderImplementation : IBIOrder
{
    // --- CRUD Operations ---
    public void Create(BO.Order order) => OrderManager.CreateOrder(order);
    public BO.Order Read(int id) => OrderManager.ReadOrder(id);
    public IEnumerable<BO.Order> ReadAll(Func<BO.Order, bool>? filter = null)
        => OrderManager.ReadAllOrders(filter);
    public void Update(BO.Order order) => OrderManager.UpdateOrder(order);
    public void Delete(int id) => OrderManager.DeleteOrder(id);

    // --- List Operations ---
    public IEnumerable<BO.OrderInList> GetOrderList() => OrderManager.GetOrderList();

    // --- Specific Operations ---
    public void AssociateCourierToOrder(int orderId, int courierId)
        => OrderManager.AssociateCourierToOrder(orderId, courierId);
    public void PickUpOrder(int orderId)
        => OrderManager.PickUpOrder(orderId);
    public void DeliverOrder(int orderId)
        => OrderManager.DeliverOrder(orderId);
    public void CancelOrder(int orderId)
        => OrderManager.CancelOrder(orderId);
    public IEnumerable<BO.Order> GetAvailableOrdersForCourier(int courierId)
        => OrderManager.GetAvailableOrdersForCourier(courierId);
    public BO.OrderStatusSummary GetOrderStatusSummary()
        => OrderManager.GetOrderStatusSummary();
    public IEnumerable<BO.DeliveryPerOrderInList> GetDeliveryHistoryForOrder(int orderId)
        => OrderManager.GetDeliveryHistoryForOrderPublic(orderId);

    #region Stage 5 - Observer Pattern Implementation

    public void AddObserver(Action listObserver) =>
        OrderManager.Observers.AddListObserver(listObserver);

    public void AddObserver(int id, Action observer) =>
        OrderManager.Observers.AddObserver(id, observer);

    public void RemoveObserver(Action listObserver) =>
        OrderManager.Observers.RemoveListObserver(listObserver);

    public void RemoveObserver(int id, Action observer) =>
        OrderManager.Observers.RemoveObserver(id, observer);

    #endregion Stage 5

    #region Stage 7 - Async Network Operations

    public async Task<(bool success, string? errorMessage, int geocodeStatus)> CreateOrderAsync(BO.Order order)
    {
        var (success, error, status) = await OrderManager.CreateOrderAsync(order);
        return (success, error, (int)status);
    }

    public async Task<(bool success, string? errorMessage, int geocodeStatus)> UpdateOrderAsync(BO.Order order, string? originalAddress)
    {
        var (success, error, status) = await OrderManager.UpdateOrderAsync(order, originalAddress);
        return (success, error, (int)status);
    }

    public async Task<IEnumerable<BO.Order>> GetAvailableOrdersWithDistanceAsync(int courierId)
    {
        return await OrderManager.GetAvailableOrdersWithRouteDistanceAsync(courierId);
    }

    public async Task<IEnumerable<BO.OrderInList>> GetOrderListWithDistancesAsync()
    {
        return await OrderManager.GetOrderListWithRouteDistancesAsync();
    }

    #endregion Stage 7
}
