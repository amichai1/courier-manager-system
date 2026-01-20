using DalApi;
using BO;
using DO;
using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using System.Threading.Tasks;

namespace BL.Helpers;

internal static class CourierManager
{
    private static readonly IDal s_dal = DalApi.Factory.Get;
    internal static ObserverManager Observers = new(); // Stage 5

    private static readonly AsyncMutex s_simulationMutex = new(); //stage 7
    private static readonly Random s_rand = new();

    // ------------------------------------
    // --- 1. CONVERSION (Mappers) ---
    // ------------------------------------

    /// <summary>
    /// Converts DO.Courier to BO.Courier with calculated fields.
    /// </summary>
    private static BO.Courier ConvertDOToBO(DO.Courier doCourier)
    {
        // Count orders assigned to this courier that are in progress
        int ordersInDelivery = 0;
        BO.OrderInProgress? currentOrder = null;

        try
        {
            // LINQ Method Syntax - demonstrates: Where, Any, FirstOrDefault with lambda
            var allCourierOrders = s_dal.Order.ReadAll()
                .Where(o => o.CourierId == doCourier.Id)
                .ToList();

            // Orders picked up but not delivered (actively being delivered)
            var inProgressOrders = allCourierOrders
                .Where(o => o.PickupDate.HasValue && !o.DeliveryDate.HasValue)
                .ToList();

            // Orders associated but not picked up yet (waiting to be picked up)
            var queuedOrders = allCourierOrders
                .Where(o => o.CourierAssociatedDate.HasValue && !o.PickupDate.HasValue)
                .ToList();

            // Count all orders in delivery (both in progress and queued)
            ordersInDelivery = inProgressOrders.Count + queuedOrders.Count;

            // Set current order - prioritize in-progress, then queued
            currentOrder = inProgressOrders.FirstOrDefault() is DO.Order firstInProgress
                ? CreateOrderInProgress(firstInProgress)
                : queuedOrders.FirstOrDefault() is DO.Order firstQueued
                    ? CreateOrderInProgress(firstQueued)
                    : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to fetch courier orders for {doCourier.Id}: {ex.Message}");
            ordersInDelivery = 0;
            currentOrder = null;
        }

        return new BO.Courier()
        {
            Id = doCourier.Id,
            Name = doCourier.Name,
            Phone = doCourier.Phone,
            Email = doCourier.Email,
            Password = doCourier.Password,
            IsActive = doCourier.IsActive,
            MaxDeliveryDistance = doCourier.MaxDeliveryDistance,
            DeliveryType = (BO.DeliveryType)doCourier.DeliveryType,
            StartWorkingDate = doCourier.StartWorkingDate,
            Status = doCourier.IsActive ? BO.CourierStatus.Available : BO.CourierStatus.Inactive,
            Location = new BO.Location()
            {
                Latitude = doCourier.AddressLatitude,
                Longitude = doCourier.AddressLongitude
            },
            DeliveredOnTime = 0,
            DeliveredLate = 0,
            CurrentOrder = currentOrder,
            TotalWeightInDelivery = 0,
            OrdersInDelivery = ordersInDelivery
        };
    }

    /// <summary>
    /// Helper method to create OrderInProgress from DO.Order.
    /// </summary>
    private static BO.OrderInProgress CreateOrderInProgress(DO.Order doOrder)
    {
        return new BO.OrderInProgress
        {
            OrderId = doOrder.Id,
            CustomerName = doOrder.CustomerName,
            CustomerPhone = doOrder.CustomerPhone,
            Address = doOrder.Address
        };
    }

    private static DO.Courier ConvertBOToDO(BO.Courier boCourier)
    {
        return new DO.Courier(
            boCourier.Id,
            boCourier.Name,
            boCourier.Phone,
            boCourier.Email,
            boCourier.Password,
            boCourier.IsActive,
            boCourier.MaxDeliveryDistance,
            (DO.DeliveryType)boCourier.DeliveryType,
            boCourier.StartWorkingDate,
            boCourier.Location!.Latitude,
            boCourier.Location!.Longitude
        );
    }

    // ------------------------------------
    // --- 1.5. HELPER METHODS (Queries with LINQ) ---
    // ------------------------------------

    /// <summary>
    /// Gets active couriers using LINQ Query Syntax with where and order by.
    /// </summary>
    private static IEnumerable<BO.Courier> GetActiveCouriers()
    {
        try
        {
            // LINQ Query Syntax - demonstrates: where, order by, select
            var activeCouriers = from courier in s_dal.Courier.ReadAll()
                                where courier.IsActive
                                orderby courier.StartWorkingDate ascending
                                select ConvertDOToBO(courier);

            return activeCouriers.ToList();
        }
        catch
        {
            return new List<BO.Courier>();
        }
    }

    /// <summary>
    /// Finds available couriers for a specific delivery type.
    /// Uses LINQ Method Syntax with lambda.
    /// </summary>
    private static IEnumerable<BO.Courier> FindAvailableCouriersByType(BO.DeliveryType deliveryType)
    {
        try
        {
            // LINQ Method Syntax with lambda expressions
            return s_dal.Courier.ReadAll()
                .Where(c => c.IsActive && (DO.DeliveryType)deliveryType == c.DeliveryType)
                .Select(doCourier => ConvertDOToBO(doCourier))
                .ToList();
        }
        catch
        {
            return new List<BO.Courier>();
        }
    }

    // ------------------------------------
    // --- 2. CRUD Logic ---
    // ------------------------------------

    public static void CreateCourier(BO.Courier courier)
    {
        lock (AdminManager.BlMutex)
        {
            if (courier.Id <= 0 || string.IsNullOrWhiteSpace(courier.Name) || courier.Location is null)
                throw new BLInvalidValueException("Courier details are invalid or missing.");

            try
            {
                DO.Courier doCourier = ConvertBOToDO(courier);
                s_dal.Courier.Create(doCourier);
            }
            catch (DO.DalAlreadyExistsException ex)
            {
                throw new BLAlreadyExistsException($"Courier ID {courier.Id} already exists.", ex);
            }

            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    public static BO.Courier ReadCourier(int id)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                DO.Courier? doCourier = s_dal.Courier.Read(id);
                if (doCourier is null)
                    throw new BLDoesNotExistException($"Courier ID {id} not found.");
                return ConvertDOToBO(doCourier);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {id} not found.", ex);
            }
        }
    }

    /// <summary>
    /// Reads all couriers with optional filtering.
    /// Uses LINQ Method Syntax with Select.
    /// </summary>
    public static IEnumerable<BO.Courier> ReadAllCouriers(Func<BO.Courier, bool>? filter = null)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Method Syntax - demonstrates: Select, Where, ToList
                var boCouriers = s_dal.Courier.ReadAll()
                    .Select(doCourier => ConvertDOToBO(doCourier))
                    .ToList();

                return filter != null ? boCouriers.Where(filter).ToList() : boCouriers;
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to read couriers: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// returns a list of CourierInList entities with accurate calculations of delays according to system settings.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="BLOperationFailedException"></exception>
    public static IEnumerable<BO.CourierInList> GetCourierList()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                TimeSpan maxAllowedTime = s_dal.Config.MaxDeliveryTime;
                IEnumerable<DO.Courier> doCouriers = s_dal.Courier.ReadAll();

                return doCouriers.Select(c =>
                {
                    // Retrieve all orders belonging to this messenger
                    var courierOrders = s_dal.Order.ReadAll(o => o.CourierId == c.Id);

                    // --- Calculation: Current order (in process) ---
                    int? currentOrderId = courierOrders
                                            .Where(o => o.DeliveryDate == null)
                                            .Select(o => (int?)o.Id)
                                            .FirstOrDefault();
                    // --- Calculation: Delivered on time / late ---
                    // Only take completed orders (have a delivery date)
                    var deliveredOrders = courierOrders.Where(o => o.DeliveryDate != null);
                    int onTime = 0;
                    int late = 0;

                    foreach (var order in deliveredOrders)
                    {
                        if (order.PickupDate.HasValue)
                        {
                            DateTime pickupTime = order.PickupDate.Value;
                            DateTime actualDelivery = order.DeliveryDate!.Value;

                            // deadline = pickup time + max allowed time
                            DateTime deadline = pickupTime + maxAllowedTime;

                            if (actualDelivery <= deadline)
                                onTime++;
                            else
                                late++;
                        }
                    }

                    return new BO.CourierInList
                    {
                        Id = c.Id,
                        Name = c.Name,
                        IsActive = c.IsActive,
                        DeliveryType = (BO.DeliveryType)c.DeliveryType,
                        StartWorkingDate = c.StartWorkingDate,

                        // calculate fields:
                        CurrentIdOrder = currentOrderId,
                        DeliveredOnTime = onTime,
                        DeliveredLate = late
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to generate courier list: {ex.Message}", ex);
            }
        }
    }
    /// <summary>
    /// Updates the courier's current geographical location.
    /// </summary>
    public static void UpdateCourierLocation(int courierId, BO.Location newLocation)
    {
        lock (AdminManager.BlMutex)
        {
            if (newLocation.Latitude < -90 || newLocation.Latitude > 90 ||
                newLocation.Longitude < -180 || newLocation.Longitude > 180)
                throw new BLInvalidValueException("Invalid geographical coordinates provided.");

            try
            {
                DO.Courier? doCourier = s_dal.Courier.Read(courierId);
                if (doCourier is null)
                    throw new BLDoesNotExistException($"Courier ID {courierId} not found for location update.");

                DO.Courier updatedDoCourier = doCourier with
                {
                    AddressLatitude = newLocation.Latitude,
                    AddressLongitude = newLocation.Longitude
                };
                s_dal.Courier.Update(updatedDoCourier);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {courierId} not found for location update.", ex);
            }

            Observers.NotifyItemUpdated(courierId); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    /// <summary>
    /// Sets the courier's logical status.
    /// </summary>
    public static void SetCourierStatus(int courierId, BO.CourierStatus status)
    {
        lock (AdminManager.BlMutex)
        {
            if (status == BO.CourierStatus.OnRouteForDelivery || status == BO.CourierStatus.OnRouteForPickup)
            {
                throw new BLInvalidValueException("Cannot directly set courier status to OnRoute states.");
            }
            if (status == BO.CourierStatus.Inactive)
            {
                // בדיקה האם יש הזמנה כלשהי שמשויכת לשליח וטרם נמסרה (תאריך מסירה ריק)
                bool hasActiveOrders = s_dal.Order.ReadAll()
                    .Any(o => o.CourierId == courierId && o.DeliveryDate == null);

                if (hasActiveOrders)
                {
                    throw new BLInvalidValueException("Cannot deactivate courier while they have deliveries in progress.");
                }
            }
            try
            {
                DO.Courier? doCourier = s_dal.Courier.Read(courierId);
                if (doCourier is null)
                    throw new BLDoesNotExistException($"Courier ID {courierId} not found.");

                bool newIsActive = (status != BO.CourierStatus.Inactive);
                DO.Courier updatedCourier = doCourier with { IsActive = newIsActive };
                s_dal.Courier.Update(updatedCourier);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {courierId} not found.", ex);
            }

            Observers.NotifyItemUpdated(courierId); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    public static void DeleteCourier(int id)
    {
        lock (AdminManager.BlMutex)
        {
            DO.Courier? doCourier = null;
            try
            {
                doCourier = s_dal.Courier.Read(id);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BO.BLDoesNotExistException($"Courier ID {id} does not exist.", ex);
            }

            if (doCourier?.IsActive ?? false)
            {
                throw new BO.BLEnableDeleteACtiveCourierException($"Cannot delete an active courier.");
            }

            try
            {
                s_dal.Courier.Delete(id);
                System.Diagnostics.Debug.WriteLine($"[INFO] Courier {id} deleted successfully");
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {id} not found for deletion.", ex);
            }

            Observers.NotifyItemUpdated(id); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    public static void UpdateCourier(BO.Courier courier)
    {
        lock (AdminManager.BlMutex)
        {
            if (courier.Id <= 0)
                throw new BLInvalidValueException("Courier ID is required for update.");

            try
            {
                DO.Courier? existingDoCourier = s_dal.Courier.Read(courier.Id);
                if (existingDoCourier is null)
                    throw new BLDoesNotExistException($"Courier ID {courier.Id} not found for update.");

                DO.Courier updatedDoCourier = existingDoCourier with
                {
                    Name = !string.IsNullOrWhiteSpace(courier.Name) ? courier.Name : existingDoCourier.Name,
                    Phone = !string.IsNullOrWhiteSpace(courier.Phone) ? courier.Phone : existingDoCourier.Phone,
                    Email = !string.IsNullOrWhiteSpace(courier.Email) ? courier.Email : existingDoCourier.Email,
                    Password = !string.IsNullOrWhiteSpace(courier.Password) ? courier.Password : existingDoCourier.Password,
                    IsActive = courier.IsActive,
                    MaxDeliveryDistance = courier.MaxDeliveryDistance,
                    DeliveryType = (DO.DeliveryType)courier.DeliveryType,
                    AddressLatitude = courier.Location?.Latitude ?? existingDoCourier.AddressLatitude,
                    AddressLongitude = courier.Location?.Longitude ?? existingDoCourier.AddressLongitude
                };

                s_dal.Courier.Update(updatedDoCourier);
                System.Diagnostics.Debug.WriteLine($"[INFO] Courier {courier.Id} ({courier.Name}) updated successfully with Name={updatedDoCourier.Name}, Email={updatedDoCourier.Email}, Phone={updatedDoCourier.Phone}");
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {courier.Id} not found for update.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to update Courier ID {courier.Id}: {ex.Message}", ex);
            }

            Observers.NotifyItemUpdated(courier.Id); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    /// <summary>
    /// Partially updates a courier with only specified fields.
    /// </summary>
    public static void UpdateCourierPartial(int courierId, string? name = null, string? phone = null,
        string? email = null, string? password = null, double? maxDeliveryDistance = null,
        BO.DeliveryType? deliveryType = null, BO.Location? location = null, bool? isActive = null)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                DO.Courier? existingDoCourier = s_dal.Courier.Read(courierId);
                if (existingDoCourier is null)
                    throw new BLDoesNotExistException($"Courier ID {courierId} not found for partial update.");

                // Build updated courier with only specified fields
                DO.Courier updatedDoCourier = existingDoCourier with
                {
                    Name = !string.IsNullOrWhiteSpace(name) ? name : existingDoCourier.Name,
                    Phone = !string.IsNullOrWhiteSpace(phone) ? phone : existingDoCourier.Phone,
                    Email = !string.IsNullOrWhiteSpace(email) ? email : existingDoCourier.Email,
                    Password = !string.IsNullOrWhiteSpace(password) ? password : existingDoCourier.Password,
                    IsActive = isActive.HasValue ? isActive.Value : existingDoCourier.IsActive,
                    MaxDeliveryDistance = maxDeliveryDistance ?? existingDoCourier.MaxDeliveryDistance,
                    DeliveryType = deliveryType.HasValue ? (DO.DeliveryType)deliveryType.Value : existingDoCourier.DeliveryType,
                    AddressLatitude = location?.Latitude ?? existingDoCourier.AddressLatitude,
                    AddressLongitude = location?.Longitude ?? existingDoCourier.AddressLongitude
                };

                s_dal.Courier.Update(updatedDoCourier);
                System.Diagnostics.Debug.WriteLine($"[INFO] Courier {courierId} partially updated");
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {courierId} not found for partial update.", ex);
            }

            Observers.NotifyItemUpdated(courierId); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    // ------------------------------------
    // --- 3. SPECIFIC OPERATIONS ---
    // ------------------------------------

    /// <summary>
    /// Gets all active orders for a specific courier.
    /// Uses LINQ Query Syntax with grouping and ordering.
    /// </summary>
    public static IEnumerable<BO.Order> GetCourierActiveOrders(int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - demonstrates: where, group by, order by, select new
                var activeOrders = from order in s_dal.Order.ReadAll()
                                  where order.CourierId == courierId && !order.DeliveryDate.HasValue
                                  group order by order.CourierAssociatedDate into dateGroup
                                  orderby dateGroup.Key
                                  select dateGroup.FirstOrDefault() into selectedOrder
                                  select OrderManager.ReadOrder(selectedOrder.Id);

                return activeOrders.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get active orders for courier {courierId}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets statistics about courier performance.
    /// Uses LINQ Query Syntax with aggregate functions.
    /// </summary>
    public static (int totalDeliveries, int onTimeDeliveries, int lateDeliveries) GetCourierStats(int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - demonstrates: where, aggregate (Count)
                var allCourierDeliveries = from delivery in s_dal.Delivery.ReadAll()
                                          where delivery.CourierId == courierId && delivery.CompletionStatus.HasValue
                                          select delivery;

                var count = allCourierDeliveries.Count();
                var onTimeCount = allCourierDeliveries
                    .Count(d => d.CompletionStatus == DO.DeliveryStatus.Completed);
                var lateCount = allCourierDeliveries
                    .Count(d => d.CompletionStatus == DO.DeliveryStatus.Failed);

                return (count, onTimeCount, lateCount);
            }
            catch
            {
                return (0, 0, 0);
            }
        }
    }

    /// <summary>
    /// Calculates the average delivery time for a specific courier based on all completed deliveries.
    /// Average is calculated as the mean time between pickup and delivery dates.
    /// </summary>
    /// <param name="courierId">The ID of the courier.</param>
    /// <returns>Average delivery time formatted as "HH:mm", or "—" if no data available.</returns>
    public static string CalculateAverageDeliveryTime(int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // Get all completed orders for this courier with both pickup and delivery dates
                var completedDeliveries = s_dal.Order.ReadAll()
                    .Where(o => o.CourierId == courierId && o.DeliveryDate.HasValue && o.PickupDate.HasValue)
                    .ToList();

                if (completedDeliveries.Count == 0)
                    return "—";

                // Calculate elapsed time for each delivery
                var elapsedTimes = completedDeliveries
                    .Select(o => o.DeliveryDate!.Value - o.PickupDate!.Value)
                    .ToList();

                // Calculate total time and average
                TimeSpan totalElapsed = TimeSpan.Zero;
                foreach (var elapsed in elapsedTimes)
                {
                    totalElapsed = totalElapsed.Add(elapsed);
                }

                TimeSpan averageElapsed = TimeSpan.FromMilliseconds(
                    totalElapsed.TotalMilliseconds / elapsedTimes.Count
                );

                // Format as HH:mm
                return averageElapsed.ToString(@"hh\:mm");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to calculate average delivery time for courier {courierId}: {ex.Message}");
                return "—";
            }
        }
    }

    /// <summary>
    /// Calculates the salary for a courier for a specific time period.
    /// Uses LINQ to aggregate delivery data for salary calculation.
    /// </summary>
    public static BO.CourierSalary CalculateSalary(int courierId, DateTime periodStart, DateTime periodEnd)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // Read courier info
                BO.Courier courier = ReadCourier(courierId);
                BO.Config config = AdminManager.GetConfig();

                // Salary rates based on delivery type
                double baseHourlyRate = courier.DeliveryType switch
                {
                    BO.DeliveryType.Car => 45.0,
                    BO.DeliveryType.Motorcycle => 42.0,
                    BO.DeliveryType.Bicycle => 38.0,
                    BO.DeliveryType.OnFoot => 35.0,
                    _ => 40.0
                };

                double perDeliveryBonus = courier.DeliveryType switch
                {
                    BO.DeliveryType.Car => 8.0,
                    BO.DeliveryType.Motorcycle => 7.0,
                    BO.DeliveryType.Bicycle => 10.0,
                    BO.DeliveryType.OnFoot => 12.0,
                    _ => 8.0
                };

                // LINQ Query Syntax - get all completed deliveries in period
                var deliveriesInPeriod = from order in s_dal.Order.ReadAll()
                                         where order.CourierId == courierId
                                         where order.DeliveryDate.HasValue
                                         where order.DeliveryDate.Value >= periodStart
                                         where order.DeliveryDate.Value <= periodEnd
                                         select order;

                var deliveryList = deliveriesInPeriod.ToList();

                // Calculate on-time vs late deliveries
                int onTimeCount = 0;
                int lateCount = 0;
                double totalDistance = 0;

                foreach (var order in deliveryList)
                {
                    if (order.PickupDate.HasValue && order.DeliveryDate.HasValue)
                    {
                        TimeSpan deliveryTime = order.DeliveryDate.Value - order.PickupDate.Value;
                        if (deliveryTime <= config.MaxDeliveryTime)
                            onTimeCount++;
                        else
                            lateCount++;
                    }

                    // Calculate distance for this delivery
                    if (config.CompanyLatitude.HasValue && config.CompanyLongitude.HasValue)
                    {
                        double distance = BO.Order.CalculateAirDistance(
                            config.CompanyLatitude.Value, config.CompanyLongitude.Value,
                            order.Latitude, order.Longitude);
                        totalDistance += distance * 1.3; // Actual road distance estimate
                    }
                }

                // Calculate working hours (estimate based on deliveries)
                double hoursWorked = deliveryList.Count * 0.75; // Average 45 min per delivery
                if (hoursWorked < 20) hoursWorked = 20; // Minimum hours

                return new BO.CourierSalary
                {
                    CourierId = courierId,
                    CourierName = courier.Name,
                    BaseHourlyRate = baseHourlyRate,
                    HoursWorked = hoursWorked,
                    TotalDeliveries = deliveryList.Count,
                    OnTimeDeliveries = onTimeCount,
                    LateDeliveries = lateCount,
                    PerDeliveryBonus = perDeliveryBonus,
                    OnTimeBonusRate = 5.0, // ₪5 bonus per on-time delivery
                    TotalDistanceKm = totalDistance,
                    PerKmRate = 1.5, // ₪1.5 per km
                    LatePenaltyRate = 3.0, // ₪3 penalty per late delivery
                    TaxRate = 0.25,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd
                };
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to calculate salary for courier {courierId}: {ex.Message}", ex);
            }
        }
    }

    // ------------------------------------
    // --- 4. PERIODIC UPDATES ---
    // ------------------------------------

    /// <summary>
    /// Periodic update method called after the system clock advances.
    /// Uses LINQ to identify inactive couriers and update their status.
    /// </summary>
    public static void PeriodicCourierUpdates(DateTime oldClock, DateTime newClock)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                TimeSpan maxInactivityTime = AdminManager.GetConfig().InactivityRange;
                List<int> deactivatedCourierIds = new();

                System.Diagnostics.Debug.WriteLine($"[PERIODIC] ⏰ Clock updated: {oldClock:g} → {newClock:g} (Δ = {(newClock - oldClock).TotalDays:F1} days)");
                System.Diagnostics.Debug.WriteLine($"[PERIODIC] Inactivity threshold: {maxInactivityTime.TotalDays} days");

                var activeCouriersToCheck = s_dal.Courier.ReadAll()
                    .Where(c => c.IsActive)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[PERIODIC] Checking {activeCouriersToCheck.Count} active couriers");

                // LINQ Query Syntax - find couriers exceeding inactivity threshold
                var couriersToDeactivate = (from courier in activeCouriersToCheck
                                       let timeSinceStart = newClock - courier.StartWorkingDate
                                       where courier.StartWorkingDate.Year > 1900
                                       where timeSinceStart > maxInactivityTime
                                       select courier).ToList();

                System.Diagnostics.Debug.WriteLine($"[PERIODIC] Found {couriersToDeactivate.Count} couriers to deactivate");

                // Update each inactive courier
                foreach (var doCourier in couriersToDeactivate)
                {
                    try
                    {
                        // Check if courier has any active orders before deactivating
                        bool hasActiveOrders = s_dal.Order.ReadAll()
                            .Any(o => o.CourierId == doCourier.Id && !o.DeliveryDate.HasValue);

                        if (!hasActiveOrders)
                        {
                            DO.Courier updatedCourier = doCourier with { IsActive = false };
                            s_dal.Courier.Update(updatedCourier);

                            System.Diagnostics.Debug.WriteLine(
                                $"[PERIODIC] ✅ Courier {doCourier.Id} ({doCourier.Name}) deactivated - " +
                                $"worked for {(newClock - doCourier.StartWorkingDate).TotalDays:F0} days " +
                                $"(threshold: {maxInactivityTime.TotalDays} days)"
                            );

                            deactivatedCourierIds.Add(doCourier.Id);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[PERIODIC] ⚠️ Courier {doCourier.Id} ({doCourier.Name}) NOT deactivated - has active orders"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to deactivate courier {doCourier.Id}: {ex.Message}");
                    }
                }

                // Notify observers AFTER all updates are complete
                if (deactivatedCourierIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[PERIODIC] 📢 Notifying observers about {deactivatedCourierIds.Count} deactivated couriers");

                    foreach (var courierId in deactivatedCourierIds)
                    {
                        Observers.NotifyItemUpdated(courierId);
                    }

                    Observers.NotifyListUpdated();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PERIODIC] ℹ️ No couriers to deactivate at this time");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error in PeriodicCourierUpdates: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Simulates courier activity periodically.
    /// Logic includes:
    /// 1. Cooldown: Couriers rest after delivery based on duration.
    /// 2. Deterministic Status: Orders are strictly categorized (50% OnTime, 45% Risk, 5% Late) based on creation time.
    /// 3. Physical Constraints: Delivery cannot happen faster than physical travel time.
    /// 4. Outcome Probabilities: 90% Success, 5% Refusal, 5% Cancellation.
    /// </summary>
    internal static async Task SimulateCourierActivityAsync()
    {
        // אם הסימולציה הקודמת עדיין רצה, דלג
        if (s_simulationMutex.CheckAndSetInProgress())
            return;

        try
        {
            List<DO.Courier> activeCouriers;
            List<DO.Order> pendingOrders;

            // שליפת נתונים בטוחה (Snapshot)
            lock (AdminManager.BlMutex)
            {
                activeCouriers = s_dal.Courier.ReadAll().Where(c => c.IsActive).ToList();
                pendingOrders = s_dal.Order.ReadAll().Where(o => !o.DeliveryDate.HasValue).ToList();
            }

            if (!activeCouriers.Any() || !pendingOrders.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[SIM] ❌ NO PENDING ORDERS FOUND!");
                await Task.Yield();
                return;
            }

            var config = AdminManager.GetConfig();
            int interval = config.SimulatorIntervalMinutes;

            foreach (var courier in activeCouriers)
            {
                try
                {
                    var currentOrder = pendingOrders.FirstOrDefault(o => o.CourierId == courier.Id);

                    // =========================================================
                    // CASE 1: שליח פנוי (מחפש עבודה)
                    // =========================================================
                    if (currentOrder is null)
                    {
                        DO.Delivery? lastDelivery = null;
                        bool isCoolingDown = false;

                        // No current order - small chance to look for one
                        if (s_rand.NextDouble() < 0.15)
                        {
                            // שליפת המשלוח האחרון שהסתיים
                            var history = s_dal.Delivery.ReadAll(d => d.CourierId == courier.Id && d.EndTime != null);
                            if (history.Any())
                                lastDelivery = history.OrderByDescending(d => d.EndTime).First();
                        }

                        if (lastDelivery != null && lastDelivery.EndTime.HasValue && lastDelivery.StartTime < lastDelivery.EndTime)
                        {
                            // חישוב זמן המנוחה: זהה לזמן שלקח המשלוח האחרון
                            TimeSpan deliveryDuration = lastDelivery.EndTime.Value - lastDelivery.StartTime;
                            DateTime freeAt = lastDelivery.EndTime.Value.Add(deliveryDuration);

                            // אם השעון הנוכחי עדיין לפני זמן השחרור - השליח נח
                            if (AdminManager.Now < freeAt)
                                isCoolingDown = true;
                        }
                            
                        if (isCoolingDown)
                            continue; // דלג לשליח הבא

                        // --- 2. הסתברות למציאת משלוח ---
                        // הנוסחה מבטיחה שאם האינטרוול גדול (למשל 100 דקות), הסיכוי יתקרב ל-100%
                        double baseFailureRate = 0.40; // 60% הצלחה לדקה בודדת
                        double adjustedSuccessRate = 1.0 - Math.Pow(baseFailureRate, Math.Max(1, interval));

                        if (s_rand.NextDouble() < adjustedSuccessRate)
                        {
                            var available = pendingOrders.Where(o => !o.CourierId.HasValue).ToList();
                            if (available.Any())
                            {
                                // Sometimes open the selection screen but not choose (50% chance)
                                if (s_rand.NextDouble() < 0.5)
                                {
                                    var chosen = available[s_rand.Next(available.Count)];
                                    try
                                    {
                                        OrderManager.AssociateCourierToOrder(chosen.Id, courier.Id);
                                        // Remove from local pending snapshot to avoid double assignment in this run
                                        pendingOrders.RemoveAll(o => o.Id == chosen.Id);
                                    }
                                    catch { /* ignore assignment failures */ }
                                }
                            }
                        }
                    }
                    else
                    {
                        // חישוב זמנים
                        DateTime startDriving = currentOrder.PickupDate ?? currentOrder.CourierAssociatedDate ?? AdminManager.Now;
                        TimeSpan drivingTimeElapsed = AdminManager.Now - startDriving; // כמה זמן הוא נוהג
                        TimeSpan totalTimeSinceCreation = AdminManager.Now - currentOrder.CreatedAt!; // כמה זמן ההזמנה קיימת

                        // --- 1. חישוב זמן פיזי נדרש (מרחק / מהירות) ---
                        double distanceKm = 0;
                        if (config.CompanyLatitude.HasValue && config.CompanyLongitude.HasValue)
                        {
                            distanceKm = BO.Order.CalculateAirDistance(
                                config.CompanyLatitude.Value, config.CompanyLongitude.Value,
                                currentOrder.Latitude, currentOrder.Longitude);
                        }

                        double speed = courier.DeliveryType switch
                        {
                            DO.DeliveryType.Car => config.CarSpeed,
                            DO.DeliveryType.Motorcycle => config.MotorcycleSpeed,
                            DO.DeliveryType.Bicycle => config.BicycleSpeed,
                            DO.DeliveryType.OnFoot => config.OnFootSpeed,
                            _ => config.CarSpeed
                        };

                        if (speed <= 0) speed = config.CarSpeed > 0 ? config.CarSpeed : 30.0;

                        // Base estimated time in minutes
                        double estimatedMinutes = (distanceKm / speed) * 60.0;
                        // Add some randomness to simulate delays (10-60 minutes)
                        double randomExtra = s_rand.Next(10, 61);
                        TimeSpan threshold = TimeSpan.FromMinutes(Math.Max(estimatedMinutes, 5) + randomExtra);

                        // --- 2. בדיקת תנאי פיזי: האם עבר מספיק זמן נסיעה ---
                        bool physicalConditionMet = drivingTimeElapsed >= threshold;

                        // --- 3. בדיקת תנאי סטטיסטי: הסתברות להצלחה ---
                        double successProbability = 0.50; // ברירת מחדל: 50% הצלחה

                        if (totalTimeSinceCreation <= config.MaxDeliveryTime)
                        {
                            successProbability = 0.90; // קרוב ל-100% הצלחה אם בזמן
                        }
                        else if (totalTimeSinceCreation <= config.MaxDeliveryTime * 1.5)
                        {
                            successProbability = 0.55; // 55% הצלחה אם מעט איחור
                        }
                        else
                        {
                            successProbability = 0.15; // 15% הצלחה אם איחור גדול
                        }

                        bool statisticalConditionMet = s_rand.NextDouble() < successProbability;

                        if (physicalConditionMet && statisticalConditionMet)
                        {
                            // --- קביעת תוצאת המסירה ---
                            double resultRoll = s_rand.NextDouble(); // הגרלה אמיתית לתוצאה
                            try
                            {
                                if (resultRoll < 0.90) // 90% Success
                                {
                                    OrderManager.DeliverOrder(currentOrder.Id);
                                }
                                else if (resultRoll < 0.95) // 5% Refusal (0.90 - 0.95)
                                {
                                    OrderManager.RefuseOrder(currentOrder.Id);
                                }
                                else // 5% Cancelled (0.95 - 1.00)
                                {
                                    OrderManager.CancelOrder(currentOrder.Id);
                                }

                                // Remove from local pending snapshot
                                pendingOrders.RemoveAll(o => o.Id == currentOrder.Id);
                            }
                            catch { /* ignore errors */ }
                        }
                        else
                        {
                            // Not enough time yet - small chance manager cancels
                            if (s_rand.NextDouble() < 0.10)
                            {
                                try
                                {
                                    OrderManager.CancelOrder(currentOrder.Id);
                                    pendingOrders.RemoveAll(o => o.Id == currentOrder.Id);
                                }
                                catch { }
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SIM ERROR] Loop courier {courier.Id}: {ex.Message}");
                }
            }

            await Task.Yield();
        }
        finally
        {
            s_simulationMutex.UnsetInProgress();
        }
    }
}
