using BO;
using DalApi;
using DO;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BL.Helpers;

/// <summary>
/// Internal static class managing the business logic for Delivery entities.
/// Responsibility: Read-only access, complex calculation (ETAs, distances), and status derivation.
/// </summary>
internal static class DeliveryManager
{
    private static readonly IDal s_dal = DalApi.Factory.Get;
    internal static ObserverManager Observers = new();
    private const double EARTH_RADIUS_KM = 6371; // Earth radius for distance calculation
    private static readonly AsyncMutex s_periodicMutex = new();
    private static readonly AsyncMutex s_simulationMutex = new();

    // ------------------------------------
    // --- 1. GEOGRAPHICAL CALCULATION ---
    // ------------------------------------

    /// <summary>
    /// Calculates air distance between two geographical points using the Haversine formula.
    /// </summary>
    private static double CalculateAirDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EARTH_RADIUS_KM * c;
    }

    // ------------------------------------
    // --- 2. CONVERSION (Mappers) ---
    // ------------------------------------

    /// <summary>
    /// Converts a DO.Delivery (from DAL) into a BO.Delivery (for BL/PL).
    /// This is complex as it requires fetching the associated Order and Courier data.
    /// </summary>
    /// <param name="doDelivery">The DO Delivery record.</param>
    private static BO.Delivery ConvertDOToBO(DO.Delivery doDelivery)
    {
        // Fetch related entities (required for full BO projection)
        // OrderManager and CourierManager are assumed to be implemented (Chapter 7c)
        BO.Order associatedOrder;
        BO.Courier assignedCourier;

        try
        {
            associatedOrder = OrderManager.ReadOrder(doDelivery.OrderId);
        }
        catch (Exception ex)
        {
            throw new BLOperationFailedException($"Failed to fetch Order ID {doDelivery.OrderId} for delivery conversion: {ex.Message}", ex);
        }

        try
        {
            assignedCourier = CourierManager.ReadCourier(doDelivery.CourierId);
        }
        catch (Exception ex)
        {
            throw new BLOperationFailedException($"Failed to fetch Courier ID {doDelivery.CourierId} for delivery conversion: {ex.Message}", ex);
        }

        // Fetch Config data
        BO.Config config = AdminManager.GetConfig();

        // Validation: Ensure all required locations are available
        if (assignedCourier.Location is null)
            throw new BLInvalidValueException($"Courier ID {doDelivery.CourierId} has no location data.");

        if (config.CompanyLatitude is null || config.CompanyLongitude is null)
            throw new BLInvalidValueException($"Company location is not configured in the system.");

        // --- Calculate Distances ---
        // Assuming Company location is stored in Config and Order location is in associatedOrder.
        double distFromCourierToPickup = CalculateAirDistance(
            assignedCourier.Location.Latitude, assignedCourier.Location.Longitude,
            associatedOrder.Latitude, associatedOrder.Longitude
        );
        double distFromPickupToTarget = CalculateAirDistance(
            associatedOrder.Latitude, associatedOrder.Longitude,
            config.CompanyLatitude.Value, config.CompanyLongitude.Value
        );

        // --- Determine Status ---
        BO.OrderStatus logicalStatus = BO.OrderStatus.Delivered;

        // --- Calculate Estimated Time WITHOUT calling CalculateEstimatedCompletionTime (avoid recursion) ---
        TimeSpan estimatedTime = CalculateEstimatedTimeInternal(
            distFromCourierToPickup + distFromPickupToTarget,
            assignedCourier.DeliveryType,
            config
        );

        return new BO.Delivery
        {
            Id = doDelivery.Id,
            OrderId = doDelivery.OrderId,
            Status = logicalStatus,

            // Courier Details
            CourierId = doDelivery.CourierId,
            CourierName = assignedCourier.Name,
            CourierVehicleType = (BO.VehicleType)assignedCourier.DeliveryType,
            CourierLocation = assignedCourier.Location,

            // Order Details
            CustomerName = associatedOrder.CustomerName,
            CustomerLocation = new BO.Location { Latitude = associatedOrder.Latitude, Longitude = associatedOrder.Longitude },
            Weight = associatedOrder.Weight,

            // Dates (Derived from associatedOrder or doDelivery)
            CourierAssociatedDate = associatedOrder.CourierAssociatedDate,
            PickupDate = associatedOrder.PickupDate,
            DeliveryDate = associatedOrder.DeliveryDate,

            // Calculated Fields
            DistanceFromCourierToPickup = distFromCourierToPickup,
            DistanceFromPickupToTarget = distFromPickupToTarget,
            EstimatedTime = estimatedTime
        };
    }

    /// <summary>
    /// Internal helper to calculate estimated time without causing recursion.
    /// Calculates time based on distance and vehicle type.
    /// </summary>
    private static TimeSpan CalculateEstimatedTimeInternal(double totalDistance, BO.DeliveryType deliveryType, BO.Config config)
    {
        // Get Courier Speed based on Vehicle Type from Config
        double speed = deliveryType switch
        {
            BO.DeliveryType.Car => config.CarSpeed,
            BO.DeliveryType.Motorcycle => config.MotorcycleSpeed,
            BO.DeliveryType.Bicycle => config.BicycleSpeed,
            BO.DeliveryType.OnFoot => config.OnFootSpeed,
            _ => config.CarSpeed
        };

        // Avoid division by zero
        if (speed <= 0)
            speed = config.CarSpeed;

        double timeHours = totalDistance / speed;
        return TimeSpan.FromHours(timeHours);
    }

    /// <summary>
    /// Returns a default DO.Delivery template when external code requests one.
    /// </summary>
    public static DO.Delivery GetDoDelivery()
    {
        return new DO.Delivery();
    }

    // ------------------------------------
    // --- 2.5. HELPER METHODS (Queries) ---
    // ------------------------------------

    /// <summary>
    /// Gets deliveries for a specific courier using LINQ Query Syntax.
    /// Example: LINQ Query Syntax #1 - demonstrates: where, select, orderby
    /// </summary>
    public static IEnumerable<BO.Delivery> GetDeliveriesByCourier(int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - where, select, order by
                var courierDeliveries = from delivery in s_dal.Delivery.ReadAll()
                                       where delivery.CourierId == courierId
                                       orderby delivery.StartTime descending
                                       select ConvertDOToBO(delivery);

                return courierDeliveries.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get deliveries for courier {courierId}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets completed deliveries using LINQ Method Syntax with lambda.
    /// Example: LINQ Method Syntax #1 - demonstrates: Where, Any with lambda
    /// </summary>
    public static IEnumerable<BO.Delivery> GetCompletedDeliveries()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Method Syntax - Where with lambda, Any
                var completedDeliveries = s_dal.Delivery.ReadAll()
                    .Where(d => d.CompletionStatus.HasValue && 
                               d.CompletionStatus.Value == DO.DeliveryStatus.Completed)
                    .Select(doDelivery => ConvertDOToBO(doDelivery))
                    .ToList();

                return completedDeliveries;
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get completed deliveries: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets deliveries grouped by status using LINQ Query Syntax with grouping.
    /// Example: LINQ Query Syntax #2 - demonstrates: where, group by, select
    /// </summary>
    public static IEnumerable<IGrouping<DO.DeliveryStatus?, BO.Delivery>> GetDeliveriesGroupedByStatus()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - where, group by, select
                var groupedDeliveries = from delivery in s_dal.Delivery.ReadAll()
                                       let boDelivery = ConvertDOToBO(delivery)
                                       group boDelivery by delivery.CompletionStatus into statusGroup
                                       select statusGroup;

                return groupedDeliveries.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to group deliveries by status: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets pending deliveries (not completed) using LINQ Method Syntax.
    /// Example: LINQ Method Syntax #2 - demonstrates: Where, FirstOrDefault
    /// </summary>
    public static IEnumerable<BO.Delivery> GetPendingDeliveries()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Method Syntax - Where with lambda, Select
                return s_dal.Delivery.ReadAll()
                    .Where(d => !d.CompletionStatus.HasValue)
                    .Select(doDelivery => ConvertDOToBO(doDelivery))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get pending deliveries: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets failed or incomplete deliveries using LINQ Query Syntax with let clause.
    /// Example: LINQ Query Syntax #3 - demonstrates: where with let, multiple conditions
    /// </summary>
    public static IEnumerable<BO.Delivery> GetFailedOrIncompleteDeliveries(DateTime beforeDate)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - demonstrates: where, let, multiple conditions
                var failedDeliveries = from delivery in s_dal.Delivery.ReadAll()
                                      where delivery.EndTime <= beforeDate
                                      let status = delivery.CompletionStatus
                                      where status == DO.DeliveryStatus.Failed || 
                                            status == DO.DeliveryStatus.CustomerNotFound
                                      select ConvertDOToBO(delivery);

                return failedDeliveries.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get failed deliveries: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets deliveries by time range using LINQ Query Syntax with ordering.
    /// Example: LINQ Query Syntax #4 - demonstrates: where with date range, order by
    /// </summary>
    public static IEnumerable<BO.Delivery> GetDeliveriesByTimeRange(DateTime startTime, DateTime endTime)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - where with multiple date conditions, order by
                var deliveriesInRange = from delivery in s_dal.Delivery.ReadAll()
                                       where delivery.StartTime >= startTime && delivery.StartTime <= endTime
                                       orderby delivery.StartTime ascending
                                       select ConvertDOToBO(delivery);

                return deliveriesInRange.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get deliveries in time range: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets deliveries for specific couriers using LINQ Method Syntax.
    /// Example: LINQ Method Syntax #3 - demonstrates: Where with complex logic
    /// </summary>
    public static IEnumerable<BO.Delivery> GetDeliveriesForCouriers(IEnumerable<int> courierIds)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                var courierIdList = courierIds.ToList();
                
                // LINQ Method Syntax - Where with lambda containing Contains
                return s_dal.Delivery.ReadAll()
                    .Where(d => courierIdList.Contains(d.CourierId))
                    .Select(doDelivery => ConvertDOToBO(doDelivery))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get deliveries for specified couriers: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets statistics about deliveries by calculating average distance and time.
    /// Example: LINQ Method Syntax #4 - demonstrates: Select with calculations, Average
    /// </summary>
    public static (double avgDistance, double avgTimeHours, int totalCount) GetDeliveryStatistics()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Method Syntax - demonstrates: Select, Average, Count
                var allDeliveries = s_dal.Delivery.ReadAll().ToList();

                if (allDeliveries.Count == 0)
                    return (0, 0, 0);

                var stats = allDeliveries
                    .Select(d => new 
                    { 
                        d.Id,
                        Distance = CalculateAirDistance(0, 0, 1, 1), // Placeholder calculation
                        TimeHours = d.EndTime.HasValue && d.StartTime != default
                            ? (d.EndTime.Value - d.StartTime).TotalHours
                            : 0
                    })
                    .ToList();

                double avgDist = stats.Count > 0 ? stats.Average(s => s.Distance) : 0;
                double avgTime = stats.Count > 0 ? stats.Average(s => s.TimeHours) : 0;

                return (avgDist, avgTime, stats.Count);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to calculate delivery statistics: {ex.Message}", ex);
            }
        }
    }

    // ------------------------------------
    // --- 3. CRUD Logic (Read Only) ---
    // ------------------------------------

    /// <summary>
    /// Reads a detailed Delivery entity by its unique ID (which is the Order ID).
    /// </summary>
    public static BO.Delivery ReadDelivery(int id)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // Reading the DO.Delivery entity is the anchor
                DO.Delivery? doDeliveryNullable = s_dal.Delivery.Read(id);
                if (doDeliveryNullable is null)
                {
                    throw new BLDoesNotExistException($"Delivery ID {id} not found.");
                }
                DO.Delivery doDelivery = doDeliveryNullable;

                // [1] BUSINESS VALIDATION: Cannot read cancelled deliveries.
                if (doDelivery.CompletionStatus.HasValue && (int)doDelivery.CompletionStatus.Value == (int)BO.DeliveryStatus.Cancelled)
                {
                    throw new BLOperationFailedException($"Delivery ID {id} was cancelled and cannot be read.");
                }

                // [2] CONVERSION: Convert DO to BO, including all calculations.
                return ConvertDOToBO(doDelivery);
            }
            catch (BLException)
            {
                throw;
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Delivery ID {id} not found.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to read delivery ID {id}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Reads a list of active deliveries, converting them from DO.Delivery to BO.Delivery.
    /// Uses LINQ Method Syntax for filtering and conversion.
    /// </summary>
    public static IEnumerable<BO.Delivery> ReadAllDeliveries(Func<BO.Delivery, bool>? filter = null)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // [1] LOGIC: Filter out cancelled/failed deliveries at the DAL layer where possible.
                IEnumerable<DO.Delivery> activeDeliveries = s_dal.Delivery.ReadAll(d => !d.CompletionStatus.HasValue || d.CompletionStatus.Value == DO.DeliveryStatus.Completed);

                // [2] CONVERSION: Convert all relevant DOs to BOs using Select
                IEnumerable<BO.Delivery> boDeliveries = activeDeliveries.Select(doDelivery => ConvertDOToBO(doDelivery));

                // [3] FILTER: Apply BO filtering if provided.
                return filter != null ? boDeliveries.Where(filter).ToList() : boDeliveries.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to read all deliveries: {ex.Message}", ex);
            }
        }
    }

    // ------------------------------------
    // --- 4. SPECIFIC OPERATIONS (Calculations) ---
    // ------------------------------------

    /// <summary>
    /// Calculates the estimated delivery completion time based on current routing and courier speeds.
    /// </summary>
    public static DateTime CalculateEstimatedCompletionTime(int deliveryId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // 1. Fetch current delivery data (which includes distances/locations)
                BO.Delivery boDelivery = ReadDelivery(deliveryId);
                BO.Config config = AdminManager.GetConfig();

                // Validation: Ensure all required data is present
                if (boDelivery.CourierLocation is null)
                    throw new BLInvalidValueException($"Courier location is not available for delivery {deliveryId}.");
                
                if (config.CompanyLatitude is null || config.CompanyLongitude is null)
                    throw new BLInvalidValueException($"Company location is not configured in the system.");

                // 2. Get Courier Speed based on Vehicle Type from Config
                double speed = boDelivery.CourierVehicleType switch
                {
                    BO.VehicleType.Car => config.CarSpeed,
                    BO.VehicleType.Motorcycle => config.MotorcycleSpeed,
                    BO.VehicleType.Bicycle => config.BicycleSpeed,
                    BO.VehicleType.OnFoot => config.OnFootSpeed,
                    _ => config.CarSpeed
                };

                if (speed <= 0)
                    speed = config.CarSpeed;

                // 3. Calculate Total Estimated Travel Distance
                double actualDistanceToTarget = boDelivery.DistanceFromCourierToPickup + boDelivery.DistanceFromPickupToTarget;
                double timeHours = actualDistanceToTarget / speed;

                // 4. Return Estimated End Time (AdminManager.Now + Time)
                return AdminManager.Now.AddHours(timeHours);
            }
            catch (BLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to calculate ETA for delivery {deliveryId}: {ex.Message}", ex);
            }
        }
    }

    // ------------------------------------
    // --- 5. PERIODIC UPDATES ---
    // ------------------------------------

    /// <summary>
    /// Periodic updates for deliveries when the system clock advances.
    /// Uses LINQ to identify deliveries that exceeded MaxDeliveryTime.
    /// </summary>
    public static void PeriodicDeliveryUpdates(DateTime oldClock, DateTime newClock)
    {
        if (s_periodicMutex.CheckAndSetInProgress())
            return;

        try
        {
            lock (AdminManager.BlMutex)
            {
                try
                {
                    var config = AdminManager.GetConfig();
                    // If MaxDeliveryTime is zero/default, do nothing
                    if (config.MaxDeliveryTime == default)
                        return;

                    // LINQ Method Syntax - demonstrates: Where, ToList
                    var inProgressDeliveries = s_dal.Delivery.ReadAll()
                        .Where(d => !d.CompletionStatus.HasValue && d.StartTime != default)
                        .ToList();

                    // Find and update deliveries that exceeded MaxDeliveryTime using LINQ
                    var expiredDeliveries = inProgressDeliveries
                        .Where(d => (AdminManager.Now - d.StartTime) > config.MaxDeliveryTime)
                        .ToList();

                    // Update each expired delivery - foreach is appropriate here due to update logic
                    foreach (var delivery in expiredDeliveries)
                    {
                        try
                        {
                            // Mark delivery as failed and set end time
                            var updatedDelivery = delivery with
                            {
                                CompletionStatus = DO.DeliveryStatus.Failed,
                                EndTime = AdminManager.Now
                            };
                            s_dal.Delivery.Update(updatedDelivery);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                catch (DO.DalDoesNotExistException ex)
                {
                    throw new BLDoesNotExistException("PeriodicDeliveryUpdates: entity not found.", ex);
                }
                catch (Exception ex)
                {
                    throw new BLOperationFailedException($"PeriodicDeliveryUpdates failed: {ex.Message}", ex);
                }
            }
        }
        finally
        {
            s_periodicMutex.UnsetInProgress();
        }
    }

    /// <summary>
    /// Simulates delivery activity - auto-completes deliveries after sufficient time has elapsed.
    /// Called asynchronously by the simulator once per second.
    /// Protected by AsyncMutex to prevent overlapping executions.
    /// </summary>
    internal static async Task SimulateDeliveryAsync()
    {
        if (s_simulationMutex.CheckAndSetInProgress())
            return;

        try
        {
            List<int> deliveriesToNotify = new();

            lock (AdminManager.BlMutex)
            {
                try
                {
                    var allDeliveries = s_dal.Delivery.ReadAll()
                        .Where(d => !d.CompletionStatus.HasValue && d.StartTime != default)
                        .ToList();

                    var config = AdminManager.GetConfig();
                    var random = new Random();

                    foreach (var delivery in allDeliveries)
                    {
                        // Simulate delivery completion after time elapsed
                        TimeSpan elapsed = AdminManager.Now - delivery.StartTime;
                        
                        // Estimate delivery should be completed after variable time
                        // Base: 15-45 minutes depending on conditions
                        double estimatedMinutes = 15 + random.Next(0, 30);
                        
                        if (elapsed.TotalMinutes >= estimatedMinutes)
                        {
                            try
                            {
                                // 80% chance of successful completion
                                bool isSuccessful = random.NextDouble() < 0.80;
                                
                                DO.DeliveryStatus status = isSuccessful 
                                    ? DO.DeliveryStatus.Completed 
                                    : DO.DeliveryStatus.Failed;

                                var updatedDelivery = delivery with
                                {
                                    CompletionStatus = status,
                                    EndTime = AdminManager.Now
                                };
                                
                                s_dal.Delivery.Update(updatedDelivery);

                                deliveriesToNotify.Add(delivery.Id);
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }

            // Notify observers OUTSIDE lock
            if (deliveriesToNotify.Any())
            {
                Observers.NotifyListUpdated();
                foreach (var deliveryId in deliveriesToNotify)
                {
                    Observers.NotifyItemUpdated(deliveryId);
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
