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
}