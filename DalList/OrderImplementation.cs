namespace Dal;
using DalApi;
using DO;

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
        return DataSource.Orders.Find(o => o.Id == id);
    }

    /// <summary>
    /// Reads all orders in the system.
    /// </summary>
    /// <returns>Copy of the list containing all orders</returns>
    public List<Order> ReadAll()
    {
        return new List<Order>(DataSource.Orders);
    }

    /// <summary>
    /// Updates an existing order in the system.
    /// </summary>
    /// <param name="item">Updated order object with valid ID</param>
    /// <exception cref="Exception">Thrown if order with the given ID does not exist</exception>
    public void Update(Order item)
    {
        var ExistItem = Read(item.Id) ?? throw new Exception($"Order with ID={item.Id} does not exists"); ;
        DataSource.Orders.Remove(ExistItem);
        DataSource.Orders.Add(item);
    }

    /// <summary>
    /// Deletes an order from the system.
    /// </summary>
    /// <param name="id">ID of the order to delete</param>
    /// <exception cref="Exception">Thrown if order with the given ID does not exist</exception>
    public void Delete(int id)
    {
        Order? orderToDelete = Read(id);
        if (orderToDelete is null)
            throw new Exception($"Order with ID {id} does not exist");
        DataSource.Orders.Remove(orderToDelete);
    }

    /// <summary>
    /// Deletes all orders from the system.
    /// </summary>
    public void DeleteAll()
    {
        DataSource.Orders.Clear();
    }
}