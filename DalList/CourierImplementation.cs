namespace Dal;
using DalApi;
using DO;
using System.Linq;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Implementation of data access methods for Courier entity.
/// Provides CRUD operations for managing courier data in the system.
/// </summary>
internal class CourierImplementation : ICourier
{
    /// <summary>
    /// Creates a new courier in the system.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Courier item)
    {
        // Check if courier with this ID already exists
        if (Read(item.Id) is not null)
            throw new DalAlreadyExistsException($"Courier with ID={item.Id} already exists"); // Replaced general Exception [cite: 265]
        // Add the courier to the list
        DataSource.Couriers.Add(item);
    }

    /// <summary>
    /// Reads a single courier by ID.
    /// </summary>
    /// <param name="id">National ID of the courier</param>
    /// <returns>Courier object if found, null otherwise</returns>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Courier? Read(int id)
    {
        // [Chapter 8a] Use FirstOrDefault (LINQ)
        return DataSource.Couriers?.FirstOrDefault(item => item?.Id == id);
    }

    /// <summary>
    /// Reads a single courier based on a general filtering function.
    /// </summary>
    /// <param name="filter">Boolean function to filter the list</param>
    /// <returns>First courier matching the filter, or null</returns>
    // [Chapter 8c] New Read method
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Courier? Read(Func<Courier, bool> filter)
    {
        // Use FirstOrDefault with the provided Func delegate
        return DataSource.Couriers?.FirstOrDefault(item => item != null && filter(item)) ?? null;

    }

    /// <summary>
    /// Reads all couriers in the system, optionally filtering by a function.
    /// </summary>
    /// <param name="filter">Optional boolean function for filtering couriers.</param>
    /// <returns>Filtered collection of couriers, or all couriers if no filter provided.</returns>
    // [Chapter 8b] Updated ReadAll signature and implementation using LINQ
    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Courier> ReadAll(Func<Courier, bool>? filter = null)
    {
        // Ensure null-coalescing operator to handle possible null reference
        return filter == null
     ? DataSource.Couriers?.Where(item => item != null).Select(item => item!) ?? Enumerable.Empty<Courier>()
     : DataSource.Couriers?.Where(item => item != null && filter(item!)).Select(item => item!) ?? Enumerable.Empty<Courier>();

    }

    /// <summary>
    /// Updates an existing courier in the system.
    /// </summary>
    /// <param name="item">Updated courier object with valid ID</param>
    /// <exception cref="Exception">Thrown if courier with the given ID does not exist</exception>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Courier item)
    {
        // Check if courier exists
        var ExistItem = Read(item.Id);
        if (ExistItem is null)
            throw new DalDoesNotExistException($"Courier with ID={item.Id} does not exists"); // Replaced general Exception [cite: 285]

        // Remove old courier using RemoveAll, then add the updated one
        DataSource.Couriers.RemoveAll(c => c?.Id == item.Id);
        // Add updated courier
        DataSource.Couriers.Add(item);
    }

    /// <summary>
        /// Deletes a courier from the system.
        /// </summary>
        /// <param name="id">National ID of the courier to delete</param>
        /// <exception cref="Exception">Thrown if courier with the given ID does not exist</exception>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        // Check if courier exist
        var CourierToDelete = Read(id);
        if (CourierToDelete is null)
            throw new DalDoesNotExistException($"Courier with ID {id} does not exist");

        // Remove courier from list
        DataSource.Couriers.RemoveAll(c => c?.Id == id);
    }

    /// <summary>
    /// Deletes all couriers from the system.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
    {
        DataSource.Couriers.Clear();
    }
}
