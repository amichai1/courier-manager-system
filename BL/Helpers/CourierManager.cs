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
            CurrentOrder = null,
            TotalWeightInDelivery = 0,
            OrdersInDelivery = 0
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
                DO.Courier doCourier = s_dal.Courier.Read(id);
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

    public static void UpdateCourier(BO.Courier courier)
    {
        lock (AdminManager.BlMutex)
        {
            if (string.IsNullOrWhiteSpace(courier.Name))
                throw new BLInvalidValueException("Courier name is required for update.");

            try
            {
                DO.Courier doCourier = ConvertBOToDO(courier);
                s_dal.Courier.Update(doCourier);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {courier.Id} not found for update.", ex);
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
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Courier ID {id} not found for deletion.", ex);
            }
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
                DO.Courier doCourier = s_dal.Courier.Read(courierId);
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
                DO.Courier doCourier = s_dal.Courier.Read(courierId);
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

    /// <summary>
    /// Periodic update method called after the system clock advances.
    /// Behavior implemented:
    /// - Use InactivityRange from config.
    /// - If courier has an in-progress delivery they remain active.
    /// - Otherwise check the most recent completed delivery EndTime; if none exist, use StartWorkingDate.
    /// - If the reference time is older than InactivityRange -> set IsActive = false.
    /// </summary>
    public static void PeriodicCourierUpdates(DateTime oldClock, DateTime newClock)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                TimeSpan inactivityRange = AdminManager.GetConfig().InactivityRange;

                // Read required DAL lists once
                var doCouriers = s_dal.Courier.ReadAll().ToList();
                var doDeliveries = s_dal.Delivery.ReadAll().ToList();

                foreach (var courier in doCouriers)
                {
                    // If courier already inactive, skip
                    if (!courier.IsActive)
                        continue;

                    // If courier currently has an in-progress delivery -> keep active
                    bool hasInProgress = doDeliveries.Any(d => d.CourierId == courier.Id && !d.CompletionStatus.HasValue);
                    if (hasInProgress)
                        continue;

                    // Find last completed delivery EndTime for this courier
                    var lastCompleted = doDeliveries
                        .Where(d => d.CourierId == courier.Id
                                    && d.CompletionStatus.HasValue
                                    && d.CompletionStatus.Value == DO.DeliveryStatus.Completed
                                    && d.EndTime.HasValue)
                        .OrderByDescending(d => d.EndTime!.Value)
                        .FirstOrDefault();

                    DateTime referenceTime = lastCompleted is not null
                        ? lastCompleted.EndTime!.Value
                        : courier.StartWorkingDate; // if no completed deliveries, use start date

                    if ((newClock - referenceTime) > inactivityRange)
                    {
                        DO.Courier updated = courier with { IsActive = false };
                        s_dal.Courier.Update(updated);
                    }
                }
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException("PeriodicCourierUpdates: entity not found.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"PeriodicCourierUpdates failed: {ex.Message}", ex);
            }
        }
    }
}