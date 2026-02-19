namespace BlApi;
using Helpers;
using BO;
using System;
using System.Collections.Generic;
using BIApi; // Add this

/// <summary>
/// Defines the service contract for managing Delivery entities.
/// Delivery objects are usually complex and read-only from the PL perspective.
/// </summary>
public interface IBIDelivery : BIApi.IObservable
{
    /// <summary>
    /// Reads a detailed Delivery entity by its unique ID.
    /// </summary>
    /// <param name="id">The unique Delivery ID.</param>
    /// <returns>The detailed BO.Delivery object.</returns>
    BO.Delivery Read(int id);

    /// <summary>
    /// Reads a filtered list of Delivery entities.
    /// </summary>
    /// <param name="filter">Optional filter function based on BO.Delivery properties.</param>
    /// <returns>An enumerable collection of BO.Delivery objects.</returns>
    IEnumerable<BO.Delivery> ReadAll(Func<BO.Delivery, bool>? filter = null);

    /// <summary>
    /// Calculates the estimated delivery time for a given delivery ID based on current courier location and speeds.
    /// </summary>
    /// <param name="deliveryId">The ID of the delivery.</param>
    /// <returns>The estimated completion time as DateTime.</returns>
    DateTime CalculateEstimatedCompletionTime(int deliveryId);
}
