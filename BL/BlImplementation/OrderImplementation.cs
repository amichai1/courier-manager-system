namespace BL.BIImplementation;

using BlApi;
using BO;
using BL.Helpers;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using global::Helpers;

internal class OrderImplementation : IBIOrder
{
    // --- CRUD ---
    public void Create(BO.Order order)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        OrderManager.CreateOrder(order);
    }
    public BO.Order Read(int id) => OrderManager.ReadOrder(id);
    public IEnumerable<BO.Order> ReadAll(Func<BO.Order, bool>? filter = null) => OrderManager.ReadAllOrders(filter);
    public void Update(BO.Order order)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        OrderManager.UpdateOrder(order);
    }
    public void Delete(int id)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        OrderManager.DeleteOrder(id);
    }

    // --- List ---
    public IEnumerable<BO.OrderInList> GetOrderList() => OrderManager.GetOrderList();

    // --- Specific Operations ---
    public void AssociateCourierToOrder(int orderId, int courierId)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        OrderManager.AssociateCourierToOrder(orderId, courierId);
    }
    public void PickUpOrder(int orderId)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        OrderManager.PickUpOrder(orderId);
    }
    public void DeliverOrder(int orderId)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        OrderManager.DeliverOrder(orderId);
    }
    
    // Updated Method Call
    public void RefuseOrder(int orderId)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        OrderManager.RefuseOrder(orderId);
    }
    
    public void CancelOrder(int orderId)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        OrderManager.CancelOrder(orderId);
    }

    public IEnumerable<BO.Order> GetAvailableOrdersForCourier(int courierId) => OrderManager.GetAvailableOrdersForCourier(courierId);
    public BO.OrderStatusSummary GetOrderStatusSummary() => OrderManager.GetOrderStatusSummary();
    public IEnumerable<BO.DeliveryPerOrderInList> GetDeliveryHistoryForOrder(int orderId) => OrderManager.GetDeliveryHistoryForOrderPublic(orderId);

    #region Observers
    public void AddObserver(Action listObserver) => OrderManager.Observers.AddListObserver(listObserver);
    public void AddObserver(int id, Action observer) => OrderManager.Observers.AddObserver(id, observer);
    public void RemoveObserver(Action listObserver) => OrderManager.Observers.RemoveListObserver(listObserver);
    public void RemoveObserver(int id, Action observer) => OrderManager.Observers.RemoveObserver(id, observer);
    #endregion

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
    public async Task<IEnumerable<BO.Order>> GetAvailableOrdersWithDistanceAsync(int courierId) => await OrderManager.GetAvailableOrdersWithRouteDistanceAsync(courierId);
    public async Task<IEnumerable<BO.OrderInList>> GetOrderListWithDistancesAsync() => await OrderManager.GetOrderListWithRouteDistancesAsync();
    #endregion
}
