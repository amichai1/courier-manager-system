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
}
