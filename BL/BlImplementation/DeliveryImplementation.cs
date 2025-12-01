namespace BL.BIImplementation;

using BlApi;
using BO;
using BL.Helpers;
using System.Collections.Generic;
using System;

/// <summary>
/// Implements the IBIDelivery service contract, delegating logic to DeliveryManager.
/// </summary>
internal class DeliveryImplementation : IBIDelivery
{
    // --- CRUD Operations ---
    public BO.Delivery Read(int id)
        => DeliveryManager.ReadDelivery(id);

    public IEnumerable<BO.Delivery> ReadAll(Func<BO.Delivery, bool>? filter = null)
        => DeliveryManager.ReadAllDeliveries(filter);

    // --- Specific Operations (Calculations) ---
    public DateTime CalculateEstimatedCompletionTime(int deliveryId)
        => DeliveryManager.CalculateEstimatedCompletionTime(deliveryId);

    #region Stage 5 - Observer Pattern Implementation
    public void AddObserver(Action listObserver) =>
        DeliveryManager.Observers.AddListObserver(listObserver);

    public void AddObserver(int id, Action observer) =>
        DeliveryManager.Observers.AddObserver(id, observer);

    public void RemoveObserver(Action listObserver) =>
        DeliveryManager.Observers.RemoveListObserver(listObserver);

    public void RemoveObserver(int id, Action observer) =>
        DeliveryManager.Observers.RemoveObserver(id, observer);
    #endregion Stage 5
}
