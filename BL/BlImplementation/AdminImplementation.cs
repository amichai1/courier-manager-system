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
    public void SetConfig(BO.Config configuration) 
    {
        AdminManager.ThrowOnSimulatorIsRunning();
        AdminManager.SetConfig(configuration);
    }

    // Clock Management
    public DateTime GetClock() => AdminManager.Now;
    public void ForwardClock(BO.TimeUnit unit)
    {
        AdminManager.ThrowOnSimulatorIsRunning();
        DateTime now = GetClock();
        DateTime newTime = unit switch
        {
            BO.TimeUnit.Minute => now.AddMinutes(1),
            BO.TimeUnit.Hour => now.AddHours(1),
            BO.TimeUnit.Day => now.AddDays(1),
            BO.TimeUnit.Month => now.AddMonths(1),
            BO.TimeUnit.Year => now.AddYears(1),
            _ => now.AddMinutes(1)
        };

        AdminManager.UpdateClock(newTime);
    }

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

    #region Stage 7 - Simulator Control
    public void StartSimulator(int interval)  //stage 7
    {
        AdminManager.ThrowOnSimulatorIsRunning();  //stage 7
        AdminManager.StartSimulator(interval);  //stage 7
    }

    public void StopSimulator()  //stage 7
        => AdminManager.StopSimulator();  //stage 7

    public bool IsSimulatorRunning =>
        AdminManager.IsSimulatorRunning;
    #endregion Stage 7
}
