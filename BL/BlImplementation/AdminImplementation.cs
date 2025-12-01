namespace BL.BIImplementation;
using System.Collections.Generic;
using BlApi;
using BO;
using System;
using global::Helpers;

/// <summary>
/// Implements the IAdmin service contract, delegating logic to AdminManager.
/// </summary>
internal class AdminImplementation : IAdmin
{
    // Configuration Management
    public BO.Config GetConfig() => AdminManager.GetConfig();
    public void SetConfig(BO.Config configuration) => AdminManager.SetConfig(configuration);

    // Clock Management
    public DateTime GetClock() => AdminManager.Now;
    public void ForwardClock(TimeSpan interval) => AdminManager.UpdateClock(AdminManager.Now.Add(interval));

    // Database Management
    public void ResetDB() => AdminManager.ResetDB();
    public void InitializeDB() => AdminManager.InitializeDB();

    #region Stage 5 - Observer Pattern Implementation
    public void AddClockObserver(Action clockObserver) =>
        AdminManager.ClockUpdatedObservers += clockObserver;

    public void RemoveClockObserver(Action clockObserver) =>
        AdminManager.ClockUpdatedObservers -= clockObserver;

    public void AddConfigObserver(Action configObserver) =>
        AdminManager.ConfigUpdatedObservers += configObserver;

    public void RemoveConfigObserver(Action configObserver) =>
        AdminManager.ConfigUpdatedObservers -= configObserver;
    #endregion Stage 5
}
