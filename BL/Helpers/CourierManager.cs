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
    internal static ObserverManager Observers = new(); // Stage 5

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
                bool courierUpdated = false; // Stage 5
                
                // LINQ Method Syntax - demonstrates: Where with lambda, ToList
                var activeCouriersToCheck = s_dal.Courier.ReadAll()
                    .Where(c => c.IsActive)
                    .ToList();

                // LINQ Query Syntax - demonstrates: where with complex condition, select
                var couriersToDeactivate = (from courier in activeCouriersToCheck
                                       let timeSinceStart = newClock - courier.StartWorkingDate
                                       where timeSinceStart > maxInactivityTime
                                       select courier).ToList();

                // Update each inactive courier
                foreach (var doCourier in couriersToDeactivate)
                {
                    try
                    {
                        DO.Courier updatedCourier = doCourier with { IsActive = false };
                        s_dal.Courier.Update(updatedCourier);
                        System.Diagnostics.Debug.WriteLine($"[INFO] Courier {doCourier.Id} marked as Inactive - worked for more than {maxInactivityTime.TotalDays} days");
                        courierUpdated = true; // Stage 5
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to deactivate courier {doCourier.Id}: {ex.Message}");
                    }
                }

                if (courierUpdated) // Stage 5
                {
                    Observers.NotifyListUpdated(); // Stage 5
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error in PeriodicCourierUpdates: {ex.Message}");
            }
        }
    }
}
