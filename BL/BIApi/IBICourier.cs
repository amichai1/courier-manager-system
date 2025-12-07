namespace BlApi;
using Helpers;
using BO;
using System.Collections.Generic;
using BIApi; // Add this

/// <summary>
/// Defines the service contract for managing Courier entities.
/// </summary>
public interface IBICourier : BIApi.IObservable // Stage 5 - Extend IObservable (use fully qualified name)
{
    // CRUD Operations
    void Create(BO.Courier courier);
    BO.Courier Read(int id);
    IEnumerable<BO.Courier> ReadAll(Func<BO.Courier, bool>? filter = null);
    IEnumerable<BO.CourierInList> GetCourierList();
    void Update(BO.Courier courier);
    void Delete(int id);

    // Specific Operations (Examples)
    void UpdateLocation(int courierId, BO.Location newLocation);
    void SetCourierStatus(int courierId, CourierStatus status);
}
