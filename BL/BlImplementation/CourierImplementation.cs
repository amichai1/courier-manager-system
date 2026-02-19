namespace BL.BIImplementation;

using BlApi;
using BO;
using BL.Helpers;
using System.Collections.Generic;
using System;
using global::Helpers;

/// <summary>
/// Implements the IBICourier service contract, delegating logic to CourierManager.
/// </summary>
internal class CourierImplementation : IBICourier
{
    // --- CRUD Operations ---
    public void Create(BO.Courier courier) 
    {
        AdminManager.ThrowOnSimulatorIsRunning();        CourierManager.CreateCourier(courier);
    }

    public BO.Courier Read(int id) => CourierManager.ReadCourier(id);
    
    public IEnumerable<BO.Courier> ReadAll(Func<BO.Courier, bool>? filter = null)
        => CourierManager.ReadAllCouriers(filter);
        
    public IEnumerable<BO.CourierInList> GetCourierList() => CourierManager.GetCourierList();
    
    public void Update(BO.Courier courier) 
    {
        AdminManager.ThrowOnSimulatorIsRunning();        CourierManager.UpdateCourier(courier);
    }
    
    public void Delete(int id) 
    {
        AdminManager.ThrowOnSimulatorIsRunning();        CourierManager.DeleteCourier(id);
    }

    // --- Specific Operations ---
    public void UpdateLocation(int courierId, BO.Location newLocation)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        CourierManager.UpdateCourierLocation(courierId, newLocation);
    }
        
    public void SetCourierStatus(int courierId, BO.CourierStatus status)
    {
        AdminManager.ThrowOnSimulatorIsRunning();        CourierManager.SetCourierStatus(courierId, status);
    }

    /// <summary>
    /// Calculates and returns the average delivery time for a courier based on all completed deliveries.
    /// </summary>
    /// <param name="courierId">The ID of the courier.</param>
    /// <returns>Average delivery time in "HH:mm" format, or "—" if no data is available.</returns>
    public string CalculateAverageDeliveryTime(int courierId)
        => CourierManager.CalculateAverageDeliveryTime(courierId);

    public BO.CourierSalary CalculateSalary(int courierId, DateTime periodStart, DateTime periodEnd)
        => CourierManager.CalculateSalary(courierId, periodStart, periodEnd);

    public void AddObserver(Action listObserver) =>
        CourierManager.Observers.AddListObserver(listObserver);

    public void AddObserver(int id, Action observer) =>
        CourierManager.Observers.AddObserver(id, observer);

    public void RemoveObserver(Action listObserver) =>
        CourierManager.Observers.RemoveListObserver(listObserver);

    public void RemoveObserver(int id, Action observer) =>
        CourierManager.Observers.RemoveObserver(id, observer);
}
