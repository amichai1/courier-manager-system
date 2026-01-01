using BL.Helpers;
using BO;
using DalApi;
using DalTest;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Helpers;

namespace Helpers;

/// <summary>
/// Internal BL manager for all Application's Configuration Variables and Clock logic policies
/// </summary>
internal static class AdminManager
{
    private static readonly IDal s_dal = Factory.Get;
    internal static readonly object BlMutex = new();

    // Stage 7 fields
    private static volatile Thread? s_thread = null;
    private static volatile bool s_stop = false;
    private static int s_interval = 0;

    // Flag to suppress observer notifications during DB operations
    internal static bool SuppressObservers { get; set; } = false;

    /// <summary>
    /// Property for providing current application's clock value for any BL class that may need it
    /// </summary>
    internal static DateTime Now { get => s_dal.Config.Clock; }

    internal static event Action? ConfigUpdatedObservers;
    internal static event Action? ClockUpdatedObservers;

    // -----------------------------------------------------------
    // *** DATABASE MANAGEMENT METHODS (FIXED LOCATION) ***
    // -----------------------------------------------------------

    internal static void ResetDB()
    {
        lock (BlMutex)
        {
            SuppressObservers = true;
            try
            {
                s_dal.ResetDB();
                s_dal.Config.Clock = DateTime.Now; // Reset clock without triggering observers
            }
            finally
            {
                SuppressObservers = false;
            }

            // Notify observers once after reset is complete
            ClockUpdatedObservers?.Invoke();
            ConfigUpdatedObservers?.Invoke();
            CourierManager.Observers.NotifyListUpdated();
            OrderManager.Observers.NotifyListUpdated();
        }
    }

    internal static void InitializeDB()
    {
        lock (BlMutex)
        {
            SuppressObservers = true;
            try
            {
                DalTest.Initialization.Do();
                // Don't reset clock - keep the clock set by Initialization.Do()
                // The initialization creates orders relative to the configured clock
            }
            finally
            {
                SuppressObservers = false;
            }

            // Notify observers once after initialization is complete
            ClockUpdatedObservers?.Invoke();
            ConfigUpdatedObservers?.Invoke();
            CourierManager.Observers.NotifyListUpdated();
            OrderManager.Observers.NotifyListUpdated();
        }
    }

    // -----------------------------------------------------------
    // *** CLOCK AND CONFIG MANAGEMENT ***
    // -----------------------------------------------------------

    /// <summary>
    /// Method to update application's clock from any BL class as may be required
    /// </summary>
    /// <param name="newClock">updated clock value</param>
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void UpdateClock(DateTime newClock)
    {
        DateTime oldClock = s_dal.Config.Clock;
        s_dal.Config.Clock = newClock;

        if (!SuppressObservers)
        {
            BL.Helpers.CourierManager.PeriodicCourierUpdates(oldClock, newClock);
            BL.Helpers.OrderManager.PeriodicOrderUpdates(oldClock, newClock);
            BL.Helpers.DeliveryManager.PeriodicDeliveryUpdates(oldClock, newClock);
            BL.Helpers.OrderManager.CheckAndUpdateExpiredOrders();

            ClockUpdatedObservers?.Invoke();
        }
    }

    /// <summary>
    /// Method for providing current configuration variables values for any BL class that may need it
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static BO.Config GetConfig()
    {
        return new BO.Config()
        {
            // Manager Credentials
            ManagerId = s_dal.Config.ManagerId,
            ManagerPassword = s_dal.Config.ManagerPassword,

            // Location and Nullable Properties
            CompanyAddress = s_dal.Config.CompanyAddress,
            CompanyLatitude = s_dal.Config.CompanyLatitude,
            CompanyLongitude = s_dal.Config.CompanyLongitude,
            MaxDeliveryDistance = s_dal.Config.MaxDeliveryDistance,

            // Speeds (Double)
            CarSpeed = s_dal.Config.CarSpeed,
            MotorcycleSpeed = s_dal.Config.MotorcycleSpeed,
            BicycleSpeed = s_dal.Config.BicycleSpeed,
            OnFootSpeed = s_dal.Config.OnFootSpeed,

            // Time Ranges (TimeSpan)
            MaxDeliveryTime = s_dal.Config.MaxDeliveryTime,
            RiskRange = s_dal.Config.RiskRange,
            InactivityRange = s_dal.Config.InactivityRange,
        };
    }

    /// <summary>
    /// Method for setting current configuration variables values for any BL class that may need it
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void SetConfig(BO.Config configuration)
    {
        bool configChanged = false;

        // [1] Manager Credentials
        if (s_dal.Config.ManagerId != configuration.ManagerId)
        {
            s_dal.Config.ManagerId = configuration.ManagerId;
            configChanged = true;
        }
        if (s_dal.Config.ManagerPassword != configuration.ManagerPassword)
        {
            s_dal.Config.ManagerPassword = configuration.ManagerPassword ?? string.Empty;
            configChanged = true;
        }

        // [2] Location and Nullable Properties
        if (s_dal.Config.CompanyAddress != configuration.CompanyAddress)
        {
            s_dal.Config.CompanyAddress = configuration.CompanyAddress ?? string.Empty;
            configChanged = true;
        }
        if (s_dal.Config.CompanyLatitude != configuration.CompanyLatitude)
        {
            s_dal.Config.CompanyLatitude = configuration.CompanyLatitude;
            configChanged = true;
        }
        if (s_dal.Config.CompanyLongitude != configuration.CompanyLongitude)
        {
            s_dal.Config.CompanyLongitude = configuration.CompanyLongitude;
            configChanged = true;
        }
        if (s_dal.Config.MaxDeliveryDistance != configuration.MaxDeliveryDistance)
        {
            s_dal.Config.MaxDeliveryDistance = configuration.MaxDeliveryDistance;
            configChanged = true;
        }

        // [3] Speeds (Double)
        if (s_dal.Config.CarSpeed != configuration.CarSpeed)
        {
            s_dal.Config.CarSpeed = configuration.CarSpeed;
            configChanged = true;
        }
        if (s_dal.Config.MotorcycleSpeed != configuration.MotorcycleSpeed)
        {
            s_dal.Config.MotorcycleSpeed = configuration.MotorcycleSpeed;
            configChanged = true;
        }
        if (s_dal.Config.BicycleSpeed != configuration.BicycleSpeed)
        {
            s_dal.Config.BicycleSpeed = configuration.BicycleSpeed;
            configChanged = true;
        }
        if (s_dal.Config.OnFootSpeed != configuration.OnFootSpeed)
        {
            s_dal.Config.OnFootSpeed = configuration.OnFootSpeed;
            configChanged = true;
        }

        // [4] Time Ranges (TimeSpan)
        if (s_dal.Config.MaxDeliveryTime != configuration.MaxDeliveryTime)
        {
            s_dal.Config.MaxDeliveryTime = configuration.MaxDeliveryTime;
            configChanged = true;
        }
        if (s_dal.Config.RiskRange != configuration.RiskRange)
        {
            s_dal.Config.RiskRange = configuration.RiskRange;
            configChanged = true;
        }
        if (s_dal.Config.InactivityRange != configuration.InactivityRange)
        {
            s_dal.Config.InactivityRange = configuration.InactivityRange;
            configChanged = true;
        }

        // Calling all the observers of configuration update
        if (configChanged && !SuppressObservers)
        {
            ConfigUpdatedObservers?.Invoke();
        }
    }

    /// <summary>
    /// Forces a configuration update and notifies observers.
    /// Use this when the DB has changed significantly (like Reset/Init) and we need to sync the UI.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void UpdateConfig(BO.Config config)
    {
        SetConfig(config);
        if (!SuppressObservers)
        {
            ConfigUpdatedObservers?.Invoke();
        }
    }

    // -----------------------------------------------------------
    // *** STAGE 7 THREADING METHODS ***
    // -----------------------------------------------------------

    private static void clockRunner()
    {
        while (!s_stop)
        {
            UpdateClock(Now.AddMinutes(s_interval));
            try
            {
                Thread.Sleep(1000);
            }
            catch (ThreadInterruptedException)
            {
                // This exception is expected when calling Stop()
            }
            catch (Exception)
            {
                // Handle or log other exceptions
            }
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void Start(int interval)
    {
        if (s_thread is null)
        {
            s_interval = interval;
            s_stop = false;
            s_thread = new(clockRunner) { Name = "ClockRunner" };
            s_thread.Start();
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void Stop()
    {
        if (s_thread is not null)
        {
            s_stop = true;
            s_thread.Interrupt();
            s_thread.Name = "ClockRunner stopped";
            s_thread = null;
        }
    }
}
