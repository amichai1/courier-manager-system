using System;
using System.Runtime.CompilerServices;
using DalApi;
using BO;
using System.Threading;
using System.Threading.Tasks;
using DalTest;

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
    private static Task? _periodicTask = null;

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
            s_dal.ResetDB();
            AdminManager.UpdateClock(AdminManager.Now);
            AdminManager.SetConfig(AdminManager.GetConfig());
        }
    }

    internal static void InitializeDB()
    {
        lock (BlMutex)
        {
            DalTest.Initialization.Do();
            AdminManager.UpdateClock(AdminManager.Now);
            AdminManager.SetConfig(AdminManager.GetConfig());
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
        var oldClock = s_dal.Config.Clock;
        s_dal.Config.Clock = newClock;

        // Add calls here to any logic method that should be called periodically,
        // after each clock update. These managers must be implemented in 7c.

        // Example calls:
        // CourierManager.PeriodicCourierUpdates(oldClock, newClock); 
        // OrderManager.PeriodicOrderUpdates(oldClock, newClock);
        // DeliveryManager.PeriodicDeliveryUpdates(oldClock, newClock); 

        //Calling all the observers of clock update
        ClockUpdatedObservers?.Invoke();
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
            s_dal.Config.ManagerPassword = configuration.ManagerPassword;
            configChanged = true;
        }

        // [2] Location and Nullable Properties
        if (s_dal.Config.CompanyAddress != configuration.CompanyAddress)
        {
            s_dal.Config.CompanyAddress = configuration.CompanyAddress;
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

        //Calling all the observers of configuration update
        if (configChanged)
            ConfigUpdatedObservers?.Invoke();
    }

    // -----------------------------------------------------------
    // *** STAGE 7 THREADING METHODS ***
    // -----------------------------------------------------------

    private static void clockRunner()
    {
        while (!s_stop)
        {
            UpdateClock(Now.AddMinutes(s_interval));

            // Add calls here to any logic simulation that was required in stage 7
            // if (_simulateTask is null || _simulateTask.IsCompleted)
            //     _simulateTask = Task.Run(() => StudentManager.SimulateCourseRegistrationAndGrade()); 

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