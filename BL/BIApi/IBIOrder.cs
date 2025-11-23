namespace BlApi;

using BO;
using System.Collections.Generic;

/// <summary>
/// Defines the service contract for managing Order entities.
/// </summary>
public interface IBIOrder
{
    // CRUD Operations
    void Create(BO.Order order);
    BO.Order Read(int id);
    IEnumerable<BO.Order> ReadAll(Func<BO.Order, bool>? filter = null);
    void Update(BO.Order order);
    void Delete(int id);

    // Specific Operations 
    void AssociateCourierToOrder(int orderId, int courierId); 
    void PickUpOrder(int orderId); 
    void DeliverOrder(int orderId); 
}