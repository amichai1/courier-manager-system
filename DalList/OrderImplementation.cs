namespace Dal;
using DalApi;
using DO;
using System.Linq;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Implementation of data access methods for Order entity.
/// Provides CRUD operations for managing order data in the system.
/// </summary>
internal class OrderImplementation : IOrder
{
    /// <summary>
    /// Creates a new order in the system with auto-generated running ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Order item)
    {
        int newId = Config.NextOrderId;
        Order newOrder = item with { Id = newId };
        DataSource.Orders.Add(newOrder);
    }

    /// <summary>
    /// Reads a single order by ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Order? Read(int id)
    {
        return DataSource.Orders.FirstOrDefault(item => item?.Id == id);
    }

    /// <summary>
    /// Reads a single order based on a general filtering function.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Order? Read(Func<Order, bool> filter)
    {
        return DataSource.Orders.FirstOrDefault(item => item != null && filter(item));
    }

    /// <summary>
    /// Reads all orders in the system, optionally filtering by a function.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Order> ReadAll(Func<Order, bool>? filter = null)
    {
        return filter == null
            ? DataSource.Orders.Where(item => item != null).Select(item => item!)
            : DataSource.Orders.Where(item => item != null && filter(item!)).Select(item => item!);
    }

    /// <summary>
    /// Updates an existing order in the system.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Order item)
    {
        var ExistItem = Read(item.Id) ?? throw new DalDoesNotExistException($"Order with ID={item.Id} does not exists");
        DataSource.Orders.RemoveAll(o => o?.Id == item.Id);
        DataSource.Orders.Add(item);
    }

    /// <summary>
    /// Deletes an order from the system.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        Order? orderToDelete = Read(id);
        if (orderToDelete is null)
            throw new DalDoesNotExistException($"Order with ID {id} does not exist");
        DataSource.Orders.RemoveAll(o => o?.Id == id);
    }

    /// <summary>
    /// Deletes all orders from the system.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
    {
        DataSource.Orders.Clear();
    }
}
