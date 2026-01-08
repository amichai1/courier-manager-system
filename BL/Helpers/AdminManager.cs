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

    // Flag to track if simulation task is still running
    private static volatile bool s_isSimulationRunning = false;

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
        lock (BlMutex)
        {
            SuppressObservers = true;
            try
            {
                s_dal.ResetDB();
                s_dal.Config.Clock = DateTime.Now;
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
        lock (BlMutex)
        {
            s_dal.Config.Clock = newClock;
        }

        // Notify outside the lock to prevent deadlock with UI thread
        if (!SuppressObservers)
        {
            ClockUpdatedObservers?.Invoke();
        }
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

    /// <summary>
    /// The main simulator thread method (ClockRunner).
    /// Runs in a loop: advances clock, triggers periodic updates, runs simulation.
    /// </summary>
    private static void clockRunner()
    {
        while (!s_stop)
        {
            try
            {
                // 1. Advance clock by X minutes (set by user)
                UpdateClock(Now.AddMinutes(s_interval));

                // 2. Trigger periodic updates asynchronously (fire-and-forget)
                Task.Run(() =>
                {
                    try
                    {
                        PeriodicUpdatesSync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SIMULATOR] Periodic updates error: {ex.Message}");
                    }
                });

                // 3. Run simulation only if previous simulation finished
                if (!s_isSimulationRunning)
                {
                    s_isSimulationRunning = true;
                    Task.Run(() =>
                    {
                        try
                        {
                            SimulateRoutineOperations();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] Simulation error: {ex.Message}");
                        }
                        finally
                        {
                            s_isSimulationRunning = false;
                        }
                    });
                }

                // 4. Sleep for 1 second
                Thread.Sleep(1000);
            }
            catch (ThreadInterruptedException)
            {
                // Expected when Stop() is called
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULATOR] ClockRunner error: {ex.Message}");
            }
        }

        System.Diagnostics.Debug.WriteLine("[SIMULATOR] Thread exited");
    }

    /// <summary>
    /// Synchronous method for periodic updates (called via Task.Run).
    /// </summary>
    private static void PeriodicUpdatesSync()
    {
        try
        {
            DateTime now;
            DateTime oldClock;

            lock (BlMutex)
            {
                now = Now;
                oldClock = now.AddMinutes(-s_interval);
            }

            // Call periodic updates - they handle their own locking
            CourierManager.PeriodicCourierUpdates(oldClock, now);
            OrderManager.PeriodicOrderUpdates(oldClock, now);
            DeliveryManager.PeriodicDeliveryUpdates(oldClock, now);
            OrderManager.CheckAndUpdateExpiredOrders();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] PeriodicUpdatesSync error: {ex.Message}");
        }
    }

    /// <summary>
    /// Method for simulating routine operations (add/update/delete entities).
    /// This simulates real-world activity in the system.
    /// </summary>
    private static void SimulateRoutineOperations()
    {
        try
        {
            Random rand = new();
            int action = rand.Next(100);

            // 30% chance: Simulate order pickup
            if (action < 30)
            {
                SimulateOrderPickup(rand);
            }
            // 30% chance: Simulate order delivery
            else if (action < 60)
            {
                SimulateOrderDelivery(rand);
            }
            // 20% chance: Simulate courier location update
            else if (action < 80)
            {
                SimulateCourierLocationUpdate(rand);
            }
            // 20% chance: Simulate order assignment
            else
            {
                SimulateOrderAssignment(rand);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] SimulateRoutineOperations error: {ex.Message}");
        }
    }

    /// <summary>
    /// Simulates picking up an order that is associated but not yet picked up.
    /// </summary>
    private static void SimulateOrderPickup(Random rand)
    {
        try
        {
            List<DO.Order> ordersToPickup;

            lock (BlMutex)
            {
                ordersToPickup = s_dal.Order.ReadAll()
                    .Where(o => o.CourierAssociatedDate.HasValue && !o.PickupDate.HasValue)
                    .ToList();
            }

            if (ordersToPickup.Count > 0)
            {
                var orderToPickup = ordersToPickup[rand.Next(ordersToPickup.Count)];
                OrderManager.PickUpOrder(orderToPickup.Id);
                System.Diagnostics.Debug.WriteLine($"[SIMULATOR] Picked up order {orderToPickup.Id}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] SimulateOrderPickup error: {ex.Message}");
        }
    }

    /// <summary>
    /// Simulates delivering an order that was picked up.
    /// </summary>
    private static void SimulateOrderDelivery(Random rand)
    {
        try
        {
            List<DO.Order> ordersToDeliver;

            lock (BlMutex)
            {
                ordersToDeliver = s_dal.Order.ReadAll()
                    .Where(o => o.PickupDate.HasValue && !o.DeliveryDate.HasValue)
                    .ToList();
            }

            if (ordersToDeliver.Count > 0)
            {
                var orderToDeliver = ordersToDeliver[rand.Next(ordersToDeliver.Count)];
                OrderManager.DeliverOrder(orderToDeliver.Id);
                System.Diagnostics.Debug.WriteLine($"[SIMULATOR] Delivered order {orderToDeliver.Id}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] SimulateOrderDelivery error: {ex.Message}");
        }
    }

    /// <summary>
    /// Simulates updating a courier's location.
    /// </summary>
    private static void SimulateCourierLocationUpdate(Random rand)
    {
        try
        {
            List<DO.Courier> activeCouriers;

            lock (BlMutex)
            {
                activeCouriers = s_dal.Courier.ReadAll()
                    .Where(c => c.IsActive)
                    .ToList();
            }

            if (activeCouriers.Count > 0)
            {
                var courier = activeCouriers[rand.Next(activeCouriers.Count)];

                // Small random movement (±0.01 degrees ~ ±1km)
                double newLat = courier.AddressLatitude + (rand.NextDouble() - 0.5) * 0.02;
                double newLon = courier.AddressLongitude + (rand.NextDouble() - 0.5) * 0.02;

                CourierManager.UpdateCourierLocation(courier.Id, new BO.Location
                {
                    Latitude = newLat,
                    Longitude = newLon
                });

                System.Diagnostics.Debug.WriteLine($"[SIMULATOR] Updated courier {courier.Id} location");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] SimulateCourierLocationUpdate error: {ex.Message}");
        }
    }

    /// <summary>
    /// Simulates assigning a courier to an open order.
    /// </summary>
    private static void SimulateOrderAssignment(Random rand)
    {
        try
        {
            List<DO.Order> openOrders;
            List<DO.Courier> availableCouriers;

            lock (BlMutex)
            {
                openOrders = s_dal.Order.ReadAll()
                    .Where(o => !o.CourierId.HasValue && !o.DeliveryDate.HasValue)
                    .ToList();

                availableCouriers = s_dal.Courier.ReadAll()
                    .Where(c => c.IsActive)
                    .ToList();
            }

            if (openOrders.Count > 0 && availableCouriers.Count > 0)
            {
                var order = openOrders[rand.Next(openOrders.Count)];
                var courier = availableCouriers[rand.Next(availableCouriers.Count)];

                OrderManager.AssociateCourierToOrder(order.Id, courier.Id);
                System.Diagnostics.Debug.WriteLine($"[SIMULATOR] Assigned courier {courier.Id} to order {order.Id}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SIMULATOR] SimulateOrderAssignment error: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts the simulator thread with the specified clock interval.
    /// </summary>
    internal static void StartSimulator(int interval)
    {
        lock (BlMutex)
        {
            if (s_thread is null)
            {
                s_interval = interval;
                s_stop = false;
                s_isSimulationRunning = false;
                s_thread = new Thread(clockRunner)
                {
                    Name = "ClockRunner",
                    IsBackground = true
                };
                s_thread.Start();
                System.Diagnostics.Debug.WriteLine($"[SIMULATOR] Started with interval {interval} minutes");
            }
        }
    }

    /// <summary>
    /// Stops the simulator thread.
    /// </summary>
    internal static void StopSimulator()
    {
        Thread? threadToStop = null;

        lock (BlMutex)
        {
            if (s_thread is not null)
            {
                s_stop = true;
                threadToStop = s_thread;
                s_thread = null;
            }
        }

        // Interrupt outside the lock to avoid deadlock
        if (threadToStop is not null)
        {
            threadToStop.Interrupt();
            System.Diagnostics.Debug.WriteLine("[SIMULATOR] Stopped");
        }
    }

    /// <summary>
    /// Returns whether the simulator is currently running.
    /// </summary>
    internal static bool IsSimulatorRunning
    {
        get
        {
            lock (BlMutex)
            {
                return s_thread is not null;
            }
        }
    }
}
