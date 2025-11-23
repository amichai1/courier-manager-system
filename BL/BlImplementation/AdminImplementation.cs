namespace BL.BIImplementation;

using BlApi;
using BO;
using Helpers;
using System;
using System.Collections.Generic;
using global::Helpers;

/// <summary>
/// Implements the IAdmin service contract.
/// Delegates all configuration and clock management to the static AdminManager class.
/// </summary>
internal class AdminImplementation : IAdmin
{
    // --- Database Management ---

    public void ResetDB()
    {
        AdminManager.ResetDB();
    }

    public void InitializeDB()
    {
        AdminManager.InitializeDB();
    }

    // --- Clock Management ---

    public DateTime GetClock()
    {
        return AdminManager.Now;
    }

    public void ForwardClock(TimeSpan interval)
    {
        AdminManager.UpdateClock(AdminManager.Now.Add(interval));
    }

    // --- Configuration Management ---

    public BO.Config GetConfig()
    {
        return AdminManager.GetConfig();
    }

    public void SetConfig(BO.Config config)
    {
        AdminManager.SetConfig(config);
    }
}