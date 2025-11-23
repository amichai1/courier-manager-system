namespace BL.BIImplementation;

using BlApi;
using BO;
using BL.Helpers;
using System.Collections.Generic;
using System;

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
}