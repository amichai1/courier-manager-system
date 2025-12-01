namespace BlApi;

using BO;
using System;

/// <summary>
/// Defines the service contract for administrative operations (Clock, Configuration, Initialization).
/// </summary>
public interface IAdmin
{
    // Database Management
    void ResetDB();
    void InitializeDB();

    // Clock Management
    DateTime GetClock();
    void ForwardClock(TimeSpan interval);

    // Configuration Management
    BO.Config GetConfig();
    void SetConfig(BO.Config config);

    #region Stage 5 - Observer Pattern for Config and Clock Updates
    void AddConfigObserver(Action configObserver);
    void RemoveConfigObserver(Action configObserver);
    void AddClockObserver(Action clockObserver);
    void RemoveClockObserver(Action clockObserver);
    #endregion Stage 5
}
