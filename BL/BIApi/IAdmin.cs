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
    void ForwardClock(BO.TimeUnit interval);

    // Configuration Management
    BO.Config GetConfig();
    void SetConfig(BO.Config config);

    #region Stage 5 - Observer Pattern for Config and Clock Updates
    void AddConfigObserver(Action configObserver);
    void RemoveConfigObserver(Action configObserver);
    void AddClockObserver(Action clockObserver);
    void RemoveClockObserver(Action clockObserver);
    #endregion Stage 5

    #region Stage 7 - Simulator Control
    /// <summary>
    /// Starts the simulator with specified clock advancement interval.
    /// </summary>
    /// <param name="intervalMinutes">Minutes to advance clock each second</param>
    void StartSimulator(int intervalMinutes);

    /// <summary>
    /// Stops the simulator.
    /// </summary>
    void StopSimulator();

    /// <summary>
    /// Returns whether the simulator is currently running.
    /// </summary>
    bool IsSimulatorRunning { get; }
    #endregion Stage 7
}
