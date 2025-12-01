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
    public void Create(BO.Courier courier) => CourierManager.CreateCourier(courier);
    public BO.Courier Read(int id) => CourierManager.ReadCourier(id);
    public IEnumerable<BO.Courier> ReadAll(Func<BO.Courier, bool>? filter = null)
        => CourierManager.ReadAllCouriers(filter);
    public void Update(BO.Courier courier) => CourierManager.UpdateCourier(courier);
    public void Delete(int id) => CourierManager.DeleteCourier(id);

    // --- Specific Operations ---
    public void UpdateLocation(int courierId, BO.Location newLocation)
        => CourierManager.UpdateCourierLocation(courierId, newLocation);
    public void SetCourierStatus(int courierId, BO.CourierStatus status)
        => CourierManager.SetCourierStatus(courierId, status);

    #region Stage 5 - Observer Pattern Implementation
    public void AddObserver(Action listObserver) =>
        CourierManager.Observers.AddListObserver(listObserver);

    public void AddObserver(int id, Action observer) =>
        CourierManager.Observers.AddObserver(id, observer);

    public void RemoveObserver(Action listObserver) =>
        CourierManager.Observers.RemoveListObserver(listObserver);

    public void RemoveObserver(int id, Action observer) =>
        CourierManager.Observers.RemoveObserver(id, observer);
    #endregion Stage 5
}
