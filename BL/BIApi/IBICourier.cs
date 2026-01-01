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

    // Specific Operations
    void UpdateLocation(int courierId, BO.Location newLocation);
    void SetCourierStatus(int courierId, CourierStatus status);

    /// <summary>
    /// Calculates the average delivery time for a courier based on all completed deliveries.
    /// </summary>
    /// <param name="courierId">The ID of the courier.</param>
    /// <returns>Average delivery time in "HH:mm" format, or "—" if no data is available.</returns>
    string CalculateAverageDeliveryTime(int courierId);

    /// <summary>
    /// Calculates the salary for a courier for a specific time period.
    /// </summary>
    /// <param name="courierId">The ID of the courier.</param>
    /// <param name="periodStart">Start date of the salary period.</param>
    /// <param name="periodEnd">End date of the salary period.</param>
    /// <returns>CourierSalary object with detailed salary breakdown.</returns>
    BO.CourierSalary CalculateSalary(int courierId, DateTime periodStart, DateTime periodEnd);
}
