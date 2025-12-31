namespace BL.BIImplementation;

using BlApi;
using BO;
using BL.Helpers;
using System.Collections.Generic;
using System;
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

    // --- Specific Operations ---
    public void AssociateCourierToOrder(int orderId, int courierId)
        => OrderManager.AssociateCourierToOrder(orderId, courierId);
    public void PickUpOrder(int orderId)
        => OrderManager.PickUpOrder(orderId);
    public void DeliverOrder(int orderId)
        => OrderManager.DeliverOrder(orderId);
    public IEnumerable<BO.Order> GetAvailableOrdersForCourier(int courierId)
        => OrderManager.GetAvailableOrdersForCourier(courierId);

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
}
