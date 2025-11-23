using DalApi;
using BO;
using DO;
using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;

namespace BL.Helpers;

/// <summary>
/// Internal static class managing the business logic for Delivery entities.
/// Responsibility: Read-only access, complex calculation (ETAs, distances), and status derivation.
/// </summary>
internal static class DeliveryManager
{
    private static readonly IDal s_dal = (IDal)Factory.Get();
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
        BO.Order associatedOrder = OrderManager.ReadOrder(doDelivery.OrderId);
        BO.Courier assignedCourier = CourierManager.ReadCourier(doDelivery.CourierId);

        // Fetch Config data
        BO.Config config = AdminManager.GetConfig();

        // --- Calculate Distances ---
        // Assuming Company location is stored in Config and Order location is in associatedOrder.
        double distFromCourierToPickup = CalculateAirDistance(
            assignedCourier.Location!.Latitude, assignedCourier.Location!.Longitude,
            associatedOrder.Latitude, associatedOrder.Longitude
        );
        double distFromPickupToTarget = CalculateAirDistance(
            associatedOrder.Latitude, associatedOrder.Longitude,
            config.CompanyLatitude!.Value, config.CompanyLongitude!.Value
        );

        // --- Determine Status ---
        // Status is derived from the Order's dates/states
        BO.OrderStatus logicalStatus = BO.OrderStatus.Confirmed; // Needs actual logic

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
            EstimatedTime = CalculateEstimatedCompletionTime(doDelivery.Id) - AdminManager.Now
        };
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
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Delivery ID {id} not found.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to read delivery ID {id}.", ex);
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
            // 1. Fetch current delivery data (which includes distances/locations)
            BO.Delivery boDelivery = ReadDelivery(deliveryId);
            BO.Config config = AdminManager.GetConfig();

            // 2. Get Courier Speed based on Vehicle Type from Config
            double speed = boDelivery.CourierVehicleType switch
            {
                VehicleType.Car => config.CarSpeed,
                VehicleType.Motorcycle => config.MotorcycleSpeed,
                VehicleType.Bicycle => config.BicycleSpeed,
                VehicleType.OnFoot => config.OnFootSpeed,
                _ => config.CarSpeed
            };

            // 3. Calculate Total Estimated Travel Distance (assuming actual distance is AirDistance * SafetyFactor, 
            // but here we use the already calculated AirDistances from the BO entity)
            double actualDistanceToTarget = boDelivery.DistanceFromCourierToPickup + boDelivery.DistanceFromPickupToTarget;
            double timeHours = actualDistanceToTarget / speed;

            // 4. Return Estimated End Time (AdminManager.Now + Time)
            return AdminManager.Now.AddHours(timeHours);
        }
    }

    // ------------------------------------
    // --- 5. PERIODIC UPDATES (No Logic Required Here) ---
    // ------------------------------------

    /// <summary>
    /// Placeholder for periodic updates related to deliveries. Not used internally by the manager itself.
    /// </summary>
    public static void PeriodicDeliveryUpdates(DateTime oldClock, DateTime newClock)
    {
        // Logic for delivery status/time updates is handled externally or within specific Order/Courier logic.
        // No implementation needed here.
    }
}