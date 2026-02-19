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

    public void AddClockObserver(Action clockObserver) =>
        AdminManager.ClockUpdatedObservers += clockObserver;

    public void RemoveClockObserver(Action clockObserver) =>
        AdminManager.ClockUpdatedObservers -= clockObserver;

    public void AddConfigObserver(Action configObserver) =>
        AdminManager.ConfigUpdatedObservers += configObserver;

    public void RemoveConfigObserver(Action configObserver) =>
        AdminManager.ConfigUpdatedObservers -= configObserver;

    public void StartSimulator(int interval)
    {
        AdminManager.ThrowOnSimulatorIsRunning();
        AdminManager.StartSimulator(interval);
    }

    public void StopSimulator()
        => AdminManager.StopSimulator();

    public bool IsSimulatorRunning =>
        AdminManager.IsSimulatorRunning;
}
