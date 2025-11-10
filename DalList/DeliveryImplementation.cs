namespace Dal;
using DalApi;
using DO;
using System.Collections.Generic;

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
        Delivery? deliveryToDelete = Read(id);
        if (deliveryToDelete is null)
            throw new Exception($"Delivery with ID {id} does not exist");
        DataSource.Deliveries.Remove(deliveryToDelete);
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
        return DataSource.Deliveries.Find(d => d.Id == id);
    }

    /// <summary>
    /// Reads all deliveries in the system.
    /// </summary>
    /// <returns>Copy of the list containing all deliveries</returns>
    public List<Delivery> ReadAll()
    {
        return new List<Delivery>(DataSource.Deliveries);
    }

    /// <summary>
    /// Updates an existing delivery in the system.
    /// </summary>
    /// <param name="item">Updated delivery object with valid ID</param>
    /// <exception cref="Exception">Thrown if delivery with the given ID does not exist</exception>
    public void Update(Delivery item)
    {
        Delivery? ExistItem = Read(item.Id);
        if (ExistItem is null)
            throw new Exception($"Delivery with ID {item.Id} does not exist");
        DataSource.Deliveries.Remove(ExistItem);
        DataSource.Deliveries.Add(item);
    }
}