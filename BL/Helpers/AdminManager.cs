using BL.Helpers;
using BO;
using DalApi;
using DalTest;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Helpers;

/// <summary>
/// Internal BL manager for all Application's Configuration Variables and Clock logic policies
/// </summary>
internal static class AdminManager
{
    private static readonly IDal s_dal = DalApi.Factory.Get;
    internal static readonly object BlMutex = new();

    // Stage 7 fields - Simulator
    private static volatile Thread? s_thread = null;
    private static volatile bool s_stop = false;
    private static int s_interval = 1;

    // Mutexes for async operations (Non-blocking)
    private static readonly AsyncMutex s_periodicMutex = new();
    private static readonly AsyncMutex s_simulationMutex = new();

    // Flag to suppress observer notifications during DB operations
    internal static bool SuppressObservers { get; set; } = false;

    /// <summary>
    /// Property for providing current application's clock value for any BL class that may need it
    /// </summary>
    internal static DateTime Now { get => s_dal.Config.Clock; }

    internal static event Action? ConfigUpdatedObservers;
    internal static event Action? ClockUpdatedObservers;

    // -----------------------------------------------------------
    // *** DATABASE MANAGEMENT METHODS ***
    // -----------------------------------------------------------

    internal static void ResetDB()
    {
        ThrowOnSimulatorIsRunning();
        lock (BlMutex)
        {
            SuppressObservers = true;
            try
            {
                s_dal.ResetDB();
            }
            finally
            {
                SuppressObservers = false;
            }
        }

        // Notify outside the lock to prevent deadlock
        ClockUpdatedObservers?.Invoke();
        ConfigUpdatedObservers?.Invoke();
        CourierManager.Observers.NotifyListUpdated();
        OrderManager.Observers.NotifyListUpdated();
    }

    internal static void InitializeDB()
    {
        ThrowOnSimulatorIsRunning();
        lock (BlMutex)
        {
            SuppressObservers = true;
            try
            {
                DalTest.Initialization.Do();
            }
            finally
            {
                SuppressObservers = false;
            }
        }

        // Notify outside the lock to prevent deadlock
        ClockUpdatedObservers?.Invoke();
        ConfigUpdatedObservers?.Invoke();
        CourierManager.Observers.NotifyListUpdated();
        OrderManager.Observers.NotifyListUpdated();
    }

    // -----------------------------------------------------------
    // *** CLOCK AND CONFIG MANAGEMENT ***
    // -----------------------------------------------------------

    /// <summary>
    /// Method to update application's clock
    /// </summary>
    internal static void UpdateClock(DateTime newClock)
    {
        DateTime oldClock;
        lock (BlMutex)
        {
            oldClock = s_dal.Config.Clock;
            s_dal.Config.Clock = newClock;
        }

        // Notify outside the lock to prevent deadlock with UI thread
        if (!SuppressObservers)
        {
            ClockUpdatedObservers?.Invoke();
        }

        // Trigger periodic updates asynchronously (Using Task.Run + AsyncMutex)
        _ = Task.Run(() => PeriodicUpdatesWrapper(oldClock, newClock));
    }

    /// <summary>
    /// Method for providing current configuration variables values
    /// </summary>
    internal static BO.Config GetConfig()
    {
        lock (BlMutex)
        {
            return new BO.Config()
            {
                ManagerId = s_dal.Config.ManagerId,
                ManagerPassword = s_dal.Config.ManagerPassword,
                CompanyAddress = s_dal.Config.CompanyAddress,
                CompanyLatitude = s_dal.Config.CompanyLatitude,
                CompanyLongitude = s_dal.Config.CompanyLongitude,
                MaxDeliveryDistance = s_dal.Config.MaxDeliveryDistance,
                CarSpeed = s_dal.Config.CarSpeed,
                MotorcycleSpeed = s_dal.Config.MotorcycleSpeed,
                BicycleSpeed = s_dal.Config.BicycleSpeed,
                OnFootSpeed = s_dal.Config.OnFootSpeed,
                MaxDeliveryTime = s_dal.Config.MaxDeliveryTime,
                RiskRange = s_dal.Config.RiskRange,
                InactivityRange = s_dal.Config.InactivityRange,
            };
        }
    }

    /// <summary>
    /// Method for setting current configuration variables values
    /// </summary>
    internal static void SetConfig(BO.Config configuration)
    {
        ThrowOnSimulatorIsRunning(); // Blocking check
        bool configChanged = false;

        lock (BlMutex)
        {
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
        }

        // Notify outside the lock
        if (configChanged && !SuppressObservers)
        {
            ConfigUpdatedObservers?.Invoke();
        }
    }

    // -----------------------------------------------------------
    // *** STAGE 7: SIMULATOR THREADING METHODS ***
    // -----------------------------------------------------------

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void ThrowOnSimulatorIsRunning()
    {
        if (s_thread is not null)
            throw new BO.BLTemporaryNotAvailableException("Cannot perform the operation since Simulator is running");
    }

    /// <summary>
    /// The main simulator thread method (ClockRunner).
    /// </summary>
    private static void clockRunner()
    {
        while (!s_stop)
        {
            try
            {
                // 1. Advance clock. UpdateClock triggers PeriodicUpdates wrapper.
                DateTime nextTime = Now.AddMinutes(s_interval);
                UpdateClock(nextTime);

                // 2. Trigger Routine Simulation asynchronously
                _ = Task.Run(SimulateRoutineOperationsAsync);

                // 3. Sleep for 1 second
                Thread.Sleep(1000);
            }
            catch (ThreadInterruptedException)
            {
                // Expected when Stop() is called
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULATOR] ClockRunner error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Wrapper for periodic updates with AsyncMutex protection
    /// </summary>
    private static void PeriodicUpdatesWrapper(DateTime oldClock, DateTime newClock)
    {
        if (s_periodicMutex.CheckAndSetInProgress())
            return;

        try
        {
            // Internal managers handle their own locking (granular locks)
            CourierManager.PeriodicCourierUpdates(oldClock, newClock);
            OrderManager.PeriodicOrderUpdates(oldClock, newClock);
            DeliveryManager.PeriodicDeliveryUpdates(oldClock, newClock);
            
            CourierManager.Observers.NotifyListUpdated();
            OrderManager.Observers.NotifyListUpdated();
            DeliveryManager.Observers.NotifyListUpdated();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] PeriodicUpdatesWrapper error: {ex.Message}");
        }
        finally
        {
            s_periodicMutex.UnsetInProgress();
        }
    }

    /// <summary>
    /// Method for simulating routine operations (add/update/delete entities).
    /// </summary>
    internal static async Task SimulateRoutineOperationsAsync()
    {
        // Try to acquire lock. If busy, skip.
        if (s_simulationMutex.CheckAndSetInProgress())
           return;

        try
        {
            // Run sub-simulations in parallel and await them here so that this wrapper
            // completes only after they finish. Each manager uses its own AsyncMutex
            // so they will skip if already running.
            var tasks = new List<Task>();
            
            // Order simulation
            tasks.Add(OrderManager.SimulateOrdersAsync());
            
            // Courier simulation
            tasks.Add(CourierManager.SimulateCourierActivityAsync());
            
            // ✅ ADD THIS LINE - Delivery simulation
            tasks.Add(DeliveryManager.SimulateDeliveryAsync());

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] SimulateRoutineOperationsAsync error: {ex.Message}");
        }
        finally
        {
            s_simulationMutex.UnsetInProgress();
        }
    }

    // --- Simulation Helpers ---
    // Note: These methods fetch lists under lock, then call Manager methods.
    // Manager methods have their own locks and handle notifications locally/safely.
    // releasing lock before calling manager prevents wide-locking the DB.
    
   
    // --- Start / Stop ---

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void StartSimulator(int interval)
    {
        if (s_thread is null)
        {
            s_interval = interval;
            
            // ✅ שמור את האינטרוול בconfig כדי שיהיה זמין ב-CourierManager
            lock (BlMutex)
            {
                var config = s_dal.Config;
                config.SimulatorIntervalMinutes = interval;
            }
            
            s_stop = false;
            s_thread = new Thread(clockRunner) { Name = "ClockRunner", IsBackground = true };
            s_thread.Start();
        }
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal static void StopSimulator()
    {
        if (s_thread is not null)
        {
            s_stop = true;
            s_thread.Interrupt(); 
            s_thread = null;
        }
    }

    internal static bool IsSimulatorRunning
    {
        get { return s_thread is not null; }
    }
}
