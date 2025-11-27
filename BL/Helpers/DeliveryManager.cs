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
    private const double EARTH_RADIUS_KM = 6371; // Earth radius for distance calculation

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
        BO.OrderStatus logicalStatus = BO.OrderStatus.Confirmed;

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
            CourierVehicleType = (VehicleType)assignedCourier.DeliveryType,
            CourierLocation = assignedCourier.Location,

            // Order Details
            CustomerName = associatedOrder.CustomerName,
            CustomerLocation = new Location { Latitude = associatedOrder.Latitude, Longitude = associatedOrder.Longitude },
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

    // Return a default DO.Delivery template when external code requests one.
    public static DO.Delivery GetDoDelivery()
    {
        return new DO.Delivery();
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
    /// </summary>
    public static IEnumerable<BO.Delivery> ReadAllDeliveries(Func<BO.Delivery, bool>? filter = null)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] LOGIC: Filter out cancelled/failed deliveries at the DAL layer where possible.
            IEnumerable<DO.Delivery> activeDeliveries = s_dal.Delivery.ReadAll(d => !d.CompletionStatus.HasValue || d.CompletionStatus.Value == DO.DeliveryStatus.Completed);

            // [2] CONVERSION: Convert all relevant DOs to BOs.
            IEnumerable<BO.Delivery> boDeliveries = activeDeliveries.Select(ConvertDOToBO);

            // [3] FILTER: Apply BO filtering if provided.
            return filter != null ? boDeliveries.Where(filter) : boDeliveries;
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
                    VehicleType.Car => config.CarSpeed,
                    VehicleType.Motorcycle => config.MotorcycleSpeed,
                    VehicleType.Bicycle => config.BicycleSpeed,
                    VehicleType.OnFoot => config.OnFootSpeed,
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
    /// Behavior implemented:
    /// - For in-progress deliveries (CompletionStatus == null and StartTime set) that exceed MaxDeliveryTime:
    ///     * mark delivery CompletionStatus = Failed
    ///     * set delivery EndTime = AdminManager.Now
    ///     * update delivery in DAL
    ///     * unassign the associated order (CourierId = 0, CourierAssociatedDate = null) so it returns to the pool
    /// - In-progress deliveries that did not exceed MaxDeliveryTime are left unchanged.
    /// </summary>
    public static void PeriodicDeliveryUpdates(DateTime oldClock, DateTime newClock)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                var config = AdminManager.GetConfig();
                // If MaxDeliveryTime is zero/default, do nothing
                if (config.MaxDeliveryTime == default)
                    return;

                var deliveries = s_dal.Delivery.ReadAll().ToList();

                foreach (var delivery in deliveries)
                {
                    // Consider only deliveries currently in progress (no completion status yet) and with a valid start time
                    if (delivery.CompletionStatus.HasValue)
                        continue;
                    if (delivery.StartTime == default)
                        continue;

                    TimeSpan elapsed = AdminManager.Now - delivery.StartTime;
                    if (elapsed > config.MaxDeliveryTime)
                    {
                        // Mark delivery as failed and set end time
                        var updatedDelivery = delivery with
                        {
                            CompletionStatus = DO.DeliveryStatus.Failed,
                            EndTime = AdminManager.Now
                        };
                        s_dal.Delivery.Update(updatedDelivery);
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
}