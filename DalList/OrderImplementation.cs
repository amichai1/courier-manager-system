namespace Dal;
using DalApi;
using DO;
using System.Linq; // Required for LINQ extension methods (FirstOrDefault, Where, Select) 
using System; // Required for Exception/Func definitions

/// <summary>
/// Implementation of data access methods for Order entity.
/// Provides CRUD operations for managing order data in the system.
/// </summary>
internal class OrderImplementation : IOrder
{
    /// <summary>
    /// Creates a new order in the system with auto-generated running ID.
    /// </summary>
    /// <param name="item">Order object with all properties filled (ID will be auto-generated)</param>
    public void Create(Order item)
    {
        // Assuming Order has an auto-incremented ID, so no pre-check is needed for existence [cite: 277]
        // If this entity had a natural ID (like Student), an existence check would be required. [cite: 278, 275]
        // Note: Config.NextOrderId is assumed to exist in the DataSource/Config static class
        int newId = Config.NextOrderId;
        Order newOrder = item with { Id = newId };
        DataSource.Orders.Add(newOrder);
    }

    /// <summary>
    /// Reads a single order by ID.
    /// </summary>
    /// <param name="id">ID of the order</param>
    /// <returns>Order object if found, null otherwise</returns>
    public Order? Read(int id)
    {
        // [Chapter 8a] Using Linq To Object (FirstOrDefault) instead of List.Find [cite: 182, 185]
        return DataSource.Orders.FirstOrDefault(item => item?.Id == id);
    }

    /// <summary>
    /// Reads a single order based on a general filtering function.
    /// </summary>
    /// <param name="filter">Boolean function to filter the list</param>
    /// <returns>First order matching the filter, or null</returns>
    // [Chapter 8c] New Read method implementation
    public Order? Read(Func<Order, bool> filter)
    {
        // Uses FirstOrDefault on the list with the provided Func delegate 
        return DataSource.Orders.FirstOrDefault(item => item != null && filter(item));
    }

    /// <summary>
    /// Reads all orders in the system, optionally filtering by a function.
    /// </summary>
    /// <param name="filter">Optional boolean function for filtering orders.</param>
    /// <returns>Filtered collection of orders, or all orders if no filter provided.</returns>
    // [Chapter 8b] Updated ReadAll signature and implementation using LINQ [cite: 193, 199]
    public IEnumerable<Order> ReadAll(Func<Order, bool>? filter = null)
    {
        // If filter is null, return all items. Otherwise, apply the filter using LINQ's Where method.
        // Returns an IEnumerable<T> (lazy loading) [cite: 196]
        return filter == null
     ? DataSource.Orders.Where(item => item != null).Select(item => item!)
     : DataSource.Orders.Where(item => item != null && filter(item!)).Select(item => item!);

    }

    /// <summary>
    /// Updates an existing order in the system.
    /// </summary>
    /// <param name="item">Updated order object with valid ID</param>
    /// <exception cref="Exception">Thrown if order with the given ID does not exist</exception>
    public void Update(Order item)
    {
        // Using the updated Read(int id) method.
        // Note: This needs to be updated in Chapter 9 to throw a custom exception. [cite: 285]
        var ExistItem = Read(item.Id) ?? throw new DalDoesNotExistException($"Order with ID={item.Id} does not exists");
        DataSource.Orders.RemoveAll(o => o?.Id == item.Id); // Using RemoveAll is often cleaner for List updates
        DataSource.Orders.Add(item);
    }

    /// <summary>
    /// Deletes an order from the system.
    /// </summary>
    /// <param name="id">ID of the order to delete</param>
    /// <exception cref="Exception">Thrown if order with the given ID does not exist</exception>
    public void Delete(int id)
    {
        // Using the updated Read(int id) method.
        // Note: This needs to be updated in Chapter 9 to throw a custom exception. [cite: 285]
        Order? orderToDelete = Read(id);
        if (orderToDelete is null)
            throw new DalDoesNotExistException($"Order with ID {id} does not exist");
        DataSource.Orders.RemoveAll(o => o?.Id == id);
    }

    /// <summary>
    /// Deletes all orders from the system.
    /// </summary>
    public void DeleteAll()
    {
        DataSource.Orders.Clear();
    }
}