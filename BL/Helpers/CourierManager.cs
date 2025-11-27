using DalApi;
using BO;
using DO;
using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;

namespace BL.Helpers;

internal static class CourierManager
{
    private static readonly IDal s_dal = DalApi.Factory.Get;

    // ------------------------------------
    // --- 1. CONVERSION (Mappers) ---
    // ------------------------------------

    private static BO.Courier ConvertDOToBO(DO.Courier doCourier)
    {
        // Count orders assigned to this courier that are in progress
        int ordersInDelivery = 0;
        BO.OrderInProgress? currentOrder = null;

        try
        {
            // Get ALL orders assigned to this courier (regardless of pickup status)
            var allCourierOrders = s_dal.Order.ReadAll()
                .Where(o => o.CourierId == doCourier.Id)
                .ToList();
            
            // Filter by different statuses:
            // 1. Orders picked up but not delivered (actively being delivered)
            var inProgressOrders = allCourierOrders
                .Where(o => o.PickupDate.HasValue && !o.DeliveryDate.HasValue)
                .ToList();
            
            // 2. Orders associated but not picked up yet (waiting to be picked up)
            var queuedOrders = allCourierOrders
                .Where(o => o.CourierAssociatedDate.HasValue && !o.PickupDate.HasValue)
                .ToList();
            
            // Count all orders in delivery (both in progress and queued)
            ordersInDelivery = inProgressOrders.Count + queuedOrders.Count;
            
            // Set current order - prioritize in-progress, then queued
            if (inProgressOrders.Count > 0)
            {
                var firstOrder = inProgressOrders.First();
                currentOrder = new BO.OrderInProgress
                {
                    OrderId = firstOrder.Id,
                    CustomerName = firstOrder.CustomerName,
                    CustomerPhone = firstOrder.CustomerPhone,
                    Address = firstOrder.Address
                };
            }
            else if (queuedOrders.Count > 0)
            {
                var firstOrder = queuedOrders.First();
                currentOrder = new BO.OrderInProgress
                {
                    OrderId = firstOrder.Id,
                    CustomerName = firstOrder.CustomerName,
                    CustomerPhone = firstOrder.CustomerPhone,
                    Address = firstOrder.Address
                };
            }
        }
        catch (Exception ex)
        {
            // If we can't fetch orders, continue with default values
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
            DeliveryType = (BO.DeliveryType)(BO.VehicleType)doCourier.DeliveryType,
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

    private static DO.Courier ConvertBOToDO(BO.Courier boCourier)
    {
        return new DO.Courier(
            boCourier.Id,
            boCourier.Name!,
            boCourier.Phone!,
            boCourier.Email!,
            boCourier.Password!,
            boCourier.IsActive,
            boCourier.MaxDeliveryDistance,
            (DO.DeliveryType)boCourier.DeliveryType,
            boCourier.StartWorkingDate,
            boCourier.Location!.Latitude,
            boCourier.Location!.Longitude
        );
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

    public static IEnumerable<BO.Courier> ReadAllCouriers(Func<BO.Courier, bool>? filter = null)
    {
        lock (AdminManager.BlMutex)
        {
            IEnumerable<BO.Courier> boCouriers = s_dal.Courier.ReadAll().Select(ConvertDOToBO);
            return filter != null ? boCouriers.Where(filter) : boCouriers;
        }
    }

    /// <summary>
    /// Updates the courier's current geographical location (Latitude and Longitude) in the DAL.
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
        }
    }

    /// <summary>
    /// Sets the courier's logical status, often tied to their IsActive flag in the DO layer.
    /// </summary>
    public static void SetCourierStatus(int courierId, BO.CourierStatus status)
    {
        lock (AdminManager.BlMutex)
        {
            if (status == BO.CourierStatus.OnRouteForDelivery || status == BO.CourierStatus.OnRouteForPickup)
            {
                throw new BLInvalidValueException("Cannot directly set courier status to OnRoute states.");
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
        }
    }
    public static void DeleteCourier(int id)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                s_dal.Courier.Delete(id);
                System.Diagnostics.Debug.WriteLine($"[INFO] Courier {id} deleted successfully");
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {id} not found for deletion.", ex);
            }
        }
    }
    /// <summary>
    /// Public converter for use by other managers. Converts DO to BO without lock acquisition.
    /// </summary>
    internal static BO.Courier ConvertDOToBOPublic(DO.Courier doCourier)
    {
        // Call your existing ConvertDOToBO method
        return ConvertDOToBO(doCourier);
    }

    /// <summary>
    /// Updates specific courier fields. Only non-null values are updated.
    /// Allows partial updates without requiring all fields.
    /// </summary>
    public static void UpdateCourier(BO.Courier courier)
    {
        lock (AdminManager.BlMutex)
        {
            // Minimal validation
            if (courier.Id <= 0)
                throw new BLInvalidValueException("Courier ID is required for update.");

            try
            {
                // Read the existing courier to preserve fields not being updated
                DO.Courier? existingDoCourier = s_dal.Courier.Read(courier.Id);
                if (existingDoCourier is null)
                    throw new BLDoesNotExistException($"Courier ID {courier.Id} not found for update.");

                // Merge: Update only the fields that are provided (not null/empty)
                // This allows partial updates
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
        }
    }

    /// <summary>
    /// Partially updates a courier with only the specified fields.
    /// Use this when you want to update specific fields without affecting others.
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
        }
    }
    /// <summary>
    /// Periodic update method called after the system clock advances.
    /// Checks inactivity range against the system clock.
    /// </summary>
    public static void PeriodicCourierUpdates(DateTime oldClock, DateTime newClock)
    {
        lock (AdminManager.BlMutex)
        {
            TimeSpan maxInactivityTime = AdminManager.GetConfig().InactivityRange;
            
            // [CRITICAL FIX] Materialize the collection FIRST before modifying
            List<DO.Courier> doCouriers = s_dal.Courier.ReadAll().ToList();

            foreach (DO.Courier doCourier in doCouriers)
            {
                // Only mark as inactive if they've been inactive in RECENT times
                // The inactivity check should only apply to time SINCE the previous clock update
                if (doCourier.IsActive)
                {
                    TimeSpan timeSincePreviousUpdate = newClock - oldClock;
                    TimeSpan timeSinceLastStartTime = newClock - doCourier.StartWorkingDate;
                    
                    // Check: Has the courier been working longer than the max inactivity time?
                    // But only deactivate during THIS clock cycle if they exceeded the limit
                    if (timeSinceLastStartTime > maxInactivityTime)
                    { 
                        DO.Courier updatedCourier = doCourier with { IsActive = false };
                        s_dal.Courier.Update(updatedCourier);
                        System.Diagnostics.Debug.WriteLine($"[INFO] Courier {doCourier.Id} marked as Inactive - worked for {timeSinceLastStartTime.TotalDays} days (max: {maxInactivityTime.TotalDays})");
                    }
                }
            }
        }
    }
}