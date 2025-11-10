namespace Dal;
using DalApi;
using DO;
using System.Linq; // Required for LINQ extension methods (FirstOrDefault, Where, Select)
using System.Collections.Generic; // Kept, though System.Linq covers most list needs
using System;

/// <summary>
/// Implementation of data access methods for Delivery entity.
/// Provides CRUD operations for managing delivery data in the system.
/// </summary>
internal class DeliveryImplementation : IDelivery
{
    /// <summary>
    /// Creates a new delivery in the system with auto-generated running ID.
    /// </summary>
    /// <param name="item">Delivery object with all properties filled (ID will be auto-generated)</param>
    public void Create(Delivery item)
    {
        // Generate new running ID for the delivery
        // Note: Delivery is assumed to use an auto-incremented ID.
        int newId = Config.NextDeliveryId;

        // Create a copy of the delivery with the new ID
        Delivery newDelivery = item with { Id = newId };

        // Add the new delivery to the list
        DataSource.Deliveries.Add(newDelivery);
    }

    /// <summary>
    /// Deletes a delivery from the system.
    /// </summary>
    /// <param name="id">ID of the delivery to delete</param>
    /// <exception cref="Exception">Thrown if delivery with the given ID does not exist</exception>
    public void Delete(int id)
    {
        // Using updated Read(int id)
        Delivery? deliveryToDelete = Read(id);
        if (deliveryToDelete is null)
            throw new DalDoesNotExistException($"Delivery with ID {id} does not exist");

        // Using RemoveAll (LINQ or List extension) for clarity
        DataSource.Deliveries.RemoveAll(d => d?.Id == id);
    }

    /// <summary>
    /// Deletes all deliveries from the system.
    /// </summary>
    public void DeleteAll()
    {
        DataSource.Deliveries.Clear();
    }

    /// <summary>
    /// Reads a single delivery by ID.
    /// </summary>
    /// <param name="id">ID of the delivery</param>
    /// <returns>Delivery object if found, null otherwise</returns>
    public Delivery? Read(int id)
    {
        // [Chapter 8a] Using Linq To Object (FirstOrDefault) instead of List.Find
        return DataSource.Deliveries.FirstOrDefault(item => item?.Id == id);
    }

    /// <summary>
    /// Reads a single delivery based on a general filtering function.
    /// </summary>
    /// <param name="filter">Boolean function to filter the list</param>
    /// <returns>First delivery matching the filter, or null</returns>
    // [Chapter 8c] New Read method implementation
    public Delivery? Read(Func<Delivery, bool> filter)
    {
        // Uses FirstOrDefault on the list with the provided Func delegate
        return DataSource.Deliveries.FirstOrDefault(item => item != null && filter(item));
    }

    /// <summary>
    /// Reads all deliveries in the system, optionally filtering by a function.
    /// </summary>
    /// <param name="filter">Optional boolean function for filtering deliveries.</param>
    /// <returns>Filtered collection of deliveries, or all deliveries if no filter provided.</returns>
    // [Chapter 8b] Updated ReadAll signature and implementation using LINQ
    public IEnumerable<Delivery> ReadAll(Func<Delivery, bool>? filter = null)
    {
        // If filter is null, return all items. Otherwise, apply the filter.
        return filter == null
     ? DataSource.Deliveries.Where(item => item != null).Select(item => item!)
     : DataSource.Deliveries.Where(item => item != null && filter(item!)).Select(item => item!);

    }

    /// <summary>
    /// Updates an existing delivery in the system.
    /// </summary>
    /// <param name="item">Updated delivery object with valid ID</param>
    /// <exception cref="Exception">Thrown if delivery with the given ID does not exist</exception>
    public void Update(Delivery item)
    {
        // Using the updated Read(int id) method.
        Delivery? ExistItem = Read(item.Id);
        if (ExistItem is null)
            throw new DalDoesNotExistException($"Delivery with ID {item.Id} does not exist");

        // Remove the old object using RemoveAll and add the new one
        DataSource.Deliveries.RemoveAll(d => d?.Id == item.Id);
        DataSource.Deliveries.Add(item);
    }
}