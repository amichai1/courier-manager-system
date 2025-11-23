namespace BlApi;

using BO;
using System.Collections.Generic;

/// <summary>
/// Defines the service contract for managing Courier entities.
/// </summary>
public interface IBICourier
{
    // CRUD Operations
    void Create(BO.Courier courier);
    BO.Courier Read(int id);
    IEnumerable<BO.Courier> ReadAll(Func<BO.Courier, bool>? filter = null); // מחזיר את כל המידע (BO)
    void Update(BO.Courier courier);
    void Delete(int id);

    // Specific Operations (Examples)
    void UpdateLocation(int courierId, BO.Location newLocation);
    void SetCourierStatus(int courierId, CourierStatus status);
}
