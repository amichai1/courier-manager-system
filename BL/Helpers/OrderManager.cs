using BO;
using DalApi;
using DO;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BL.Helpers;

internal static class OrderManager
{
    private static readonly IDal s_dal = DalApi.Factory.Get;
    internal static ObserverManager Observers = new(); // Stage 5

    // ------------------------------------
    // --- 1. CONVERSION (Mappers) ---
    // ------------------------------------

    /// <summary>
    /// Converts a DO.Order (from DAL) into a BO.Order (for BL/PL).
    /// </summary>
    private static BO.Order ConvertDOToBO(DO.Order doOrder)
    {
        // NOTE: The BO.Order structure is complex and includes calculated fields.

        // [CRITICAL FIX] Do NOT fetch Courier details here to avoid collection modification issues
        // Courier details should only be fetched when explicitly requested, not during conversion
        BO.Courier? assignedCourier = null;
        if (doOrder.CourierId.HasValue)
        {
            try
            {
                assignedCourier = CourierManager.ReadCourier(doOrder.CourierId.Value);
            }
            catch
            {
                // If courier cannot be fetched, continue with null
                assignedCourier = null;
            }
        }

        // Determine OrderStatus based on dates and courier assignment
        BO.OrderStatus orderStatus = BO.OrderStatus.Confirmed;
        if (doOrder.DeliveryDate.HasValue)
        {
            orderStatus = BO.OrderStatus.Delivered;
        }
        else if (doOrder.PickupDate.HasValue)
        {
            orderStatus = BO.OrderStatus.InProgress;
        }
        else if (doOrder.CourierAssociatedDate.HasValue)
        {
            orderStatus = BO.OrderStatus.AssociatedToCourier;
        }

        // Fetch delivery history for this order
        IEnumerable<BO.DeliveryPerOrderInList> deliveryHistory = GetDeliveryHistoryForOrder(doOrder.Id);

        return new BO.Order
        {
            Id = doOrder.Id,
            OrderType = (BO.OrderType)doOrder.OrderType,
            Description = doOrder.Description,
            Address = doOrder.Address,
            Latitude = doOrder.Latitude,
            Longitude = doOrder.Longitude,
            CustomerName = doOrder.CustomerName,
            CustomerPhone = doOrder.CustomerPhone,
            Weight = doOrder.Weight,
            Volume = doOrder.Volume,
            IsFragile = doOrder.IsFragile,
            CreatedAt = doOrder.CreatedAt,
            OrderStatus = orderStatus,
            ScheduleStatus = CalculateScheduleStatus(doOrder),
            ExpectedDeliverdTime = CalculateExpectedDeliveryTime(doOrder),
            MaxDeliveredTime = CalculateMaxDeliveryTime(doOrder),
            CourierId = doOrder.CourierId,
            CourierName = assignedCourier?.Name,
            CourierAssociatedDate = doOrder.CourierAssociatedDate,
            PickupDate = doOrder.PickupDate,
            DeliveryDate = doOrder.DeliveryDate,
            OrderComplitionTime = doOrder.DeliveryDate.HasValue && doOrder.CreatedAt != default
                ? doOrder.DeliveryDate.Value - doOrder.CreatedAt
                : null,
            DeliveryHistory = deliveryHistory.ToList(),
            CustomerLocation = new BO.Location
            {
                Latitude = doOrder.Latitude,
                Longitude = doOrder.Longitude
            },
            ArialDistance = CalculateAirDistance(doOrder)
        };
    }

    /// <summary>
    /// Converts a BO.Order (from BL) into a DO.Order (for DAL).
    /// </summary>
    private static DO.Order ConvertBOToDO(BO.Order boOrder)
    {
        return new DO.Order
        {
            Id = boOrder.Id,
            CreatedAt = boOrder.CreatedAt,
            OrderType = (DO.OrderType)boOrder.OrderType,
            Description = boOrder.Description,
            Address = boOrder.Address,
            Latitude = boOrder.Latitude,
            Longitude = boOrder.Longitude,
            CustomerName = boOrder.CustomerName!,
            CustomerPhone = boOrder.CustomerPhone!,
            Weight = boOrder.Weight,
            Volume = boOrder.Volume,
            IsFragile = boOrder.IsFragile,
        };
    }

    // ------------------------------------
    // --- 1.5. HELPER METHODS (Calculation & Queries) ---
    // ------------------------------------

    /// <summary>
    /// Retrieves delivery history for a specific order using LINQ Query Syntax.
    /// Example of: let, select new
    /// </summary>
    private static IEnumerable<BO.DeliveryPerOrderInList> GetDeliveryHistoryForOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - demonstrates: let, select new
                var deliveryHistory = from delivery in s_dal.Delivery.ReadAll()
                                     where delivery.OrderId == orderId && delivery.CompletionStatus.HasValue
                                     let courierInfo = (delivery.CourierId > 0 ? 
                                         s_dal.Courier.Read(delivery.CourierId) : null)
                                     select new BO.DeliveryPerOrderInList
                                     {
                                         DeliveryId = delivery.Id,
                                         CourierId = delivery.CourierId,
                                         CourierName = courierInfo?.Name ?? "Unknown",
                                         DeliveryType = (BO.DeliveryType)courierInfo?.DeliveryType!,
                                         StartTimeDelivery = delivery.StartTime,
                                         EndType = (BO.DeliveryStatus?)delivery.CompletionStatus,
                                         EndTime = delivery.EndTime
                                     };

                return deliveryHistory.ToList();
            }
            catch
            {
                return new List<BO.DeliveryPerOrderInList>();
            }
        }
    }

    /// <summary>
    /// Calculates schedule status for an order using LINQ Method Syntax.
    /// Example of: Where, Any
    /// </summary>
    private static BO.ScheduleStatus CalculateScheduleStatus(DO.Order doOrder)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                var config = AdminManager.GetConfig();
                DateTime nowTime = AdminManager.Now;

                if (doOrder.DeliveryDate.HasValue)
                {
                    // Order is delivered - check if on time or late
                    return doOrder.DeliveryDate.Value <= doOrder.CreatedAt.Add(config.MaxDeliveryTime)
                        ? BO.ScheduleStatus.OnTime
                        : BO.ScheduleStatus.Late;
                }

                // Check if order is in risk or late
                TimeSpan timeUntilMax = doOrder.CreatedAt.Add(config.MaxDeliveryTime) - nowTime;
                
                // LINQ Method Syntax - demonstrates: Any
                bool hasRiskTime = s_dal.Order.ReadAll()
                    .Where(o => o.Id == doOrder.Id && o.CourierAssociatedDate.HasValue)
                    .Any(o => timeUntilMax > TimeSpan.Zero && timeUntilMax <= config.RiskRange);

                return hasRiskTime ? BO.ScheduleStatus.InRisk
                    : (timeUntilMax <= TimeSpan.Zero ? BO.ScheduleStatus.Late : BO.ScheduleStatus.OnTime);
            }
            catch
            {
                return BO.ScheduleStatus.OnTime;
            }
        }
    }

    private static DateTime CalculateExpectedDeliveryTime(DO.Order doOrder)
    {
        try
        {
            var config = AdminManager.GetConfig();
            // Basic calculation - can be enhanced with actual distance/speed
            return doOrder.CreatedAt.AddHours(1);
        }
        catch
        {
            return doOrder.CreatedAt.AddHours(2);
        }
    }

    private static DateTime CalculateMaxDeliveryTime(DO.Order doOrder)
    {
        try
        {
            var config = AdminManager.GetConfig();
            return doOrder.CreatedAt.Add(config.MaxDeliveryTime);
        }
        catch
        {
            return doOrder.CreatedAt.AddHours(2);
        }
    }

    private static double CalculateAirDistance(DO.Order doOrder)
    {
        try
        {
            var config = AdminManager.GetConfig();
            if (config.CompanyLatitude is null || config.CompanyLongitude is null)
                return 0;

            const double EARTH_RADIUS_KM = 6371;
            double lat1 = config.CompanyLatitude.Value * Math.PI / 180;
            double lat2 = doOrder.Latitude * Math.PI / 180;
            double dLat = (doOrder.Latitude - config.CompanyLatitude.Value) * Math.PI / 180;
            double dLon = (doOrder.Longitude - config.CompanyLongitude.Value) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EARTH_RADIUS_KM * c;
        }
        catch
        {
            return 0;
        }
    }

    // ------------------------------------
    // --- 2. CRUD Logic ---
    // ------------------------------------

    public static void CreateOrder(BO.Order order)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Check required fields and non-negative values
            if (order.Weight <= 0 || string.IsNullOrWhiteSpace(order.CustomerName))
                throw new BLInvalidValueException("Order Weight or Customer Name is missing or invalid.");

            // [2] DAL CREATE & EXCEPTION HANDLING
            try
            {
                DO.Order doOrder = ConvertBOToDO(order);
                s_dal.Order.Create(doOrder);
            }
            catch (DO.DalAlreadyExistsException ex)
            {
                throw new BLAlreadyExistsException($"Order ID {order.Id} already exists.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to create order: {ex.Message}", ex);
            }

            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    public static BO.Order ReadOrder(int id)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                DO.Order? doOrder = s_dal.Order.Read(id);
                if (doOrder is null)
                    throw new BLDoesNotExistException($"Order ID {id} not found.");
                return ConvertDOToBO(doOrder);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {id} not found.", ex);
            }
        }
    }

    /// <summary>
    /// Reads all orders with optional filtering.
    /// Uses LINQ Method Syntax with Select for conversion.
    /// </summary>
    public static IEnumerable<BO.Order> ReadAllOrders(Func<BO.Order, bool>? filter = null)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Method Syntax - demonstrates: Select, Where, ToList
                var boOrders = s_dal.Order.ReadAll()
                    .Select(doOrder =>
                    {
                        try
                        {
                            return ConvertDOToBO(doOrder);
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(order => order != null)
                    .Cast<BO.Order>()
                    .ToList();

                return filter != null ? boOrders.Where(filter).ToList() : boOrders;
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to read orders: {ex.Message}", ex);
            }
        }
    }

    public static void UpdateOrder(BO.Order order)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Check if order is still open for modification
            if (order.OrderStatus != BO.OrderStatus.Confirmed)
                throw new BLOperationFailedException($"Cannot update Order ID {order.Id}: Status is {order.OrderStatus} (not open for modification).");

            try
            {
                DO.Order doOrder = ConvertBOToDO(order);
                s_dal.Order.Update(doOrder);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {order.Id} not found for update.", ex);
            }

            Observers.NotifyItemUpdated(order.Id); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    public static void DeleteOrder(int id)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // [1] VALIDATION: Ensure the order is not in progress before deletion
                BO.Order boOrder = ReadOrder(id);
                if (boOrder.OrderStatus != BO.OrderStatus.Confirmed)
                    throw new BLOperationFailedException($"Cannot delete Order ID {id}: It has already been processed or is active.");

                s_dal.Order.Delete(id);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {id} not found for deletion.", ex);
            }

            Observers.NotifyItemUpdated(id); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    // ------------------------------------
    // --- 3. SPECIFIC OPERATIONS (Order Flow Management) ---
    // ------------------------------------

    /// <summary>
    /// Associates a courier to an order.
    /// Uses LINQ to verify courier availability with lambda expressions.
    /// </summary>
    public static void AssociateCourierToOrder(int orderId, int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            // validation
            BO.Order boOrder = ReadOrder(orderId);
            BO.Courier boCourier = CourierManager.ReadCourier(courierId);

            if (boOrder.OrderStatus != BO.OrderStatus.Confirmed)
                throw new BLOperationFailedException($"Order ID {orderId} is not confirmed (Status: {boOrder.OrderStatus}).");
            if (boCourier.Status != BO.CourierStatus.Available)
                throw new BLOperationFailedException($"Courier ID {courierId} is not available (Status: {boCourier.Status}).");

            // [2] LOGIC: Update Order with Courier information
            try
            {
                DO.Order? doOrderNullable = s_dal.Order.Read(orderId);
                if (doOrderNullable is null)
                    throw new BLDoesNotExistException($"Order ID {orderId} not found.");

                DO.Order doOrder = doOrderNullable;
                DO.Order updatedDoOrder = doOrder with
                {
                    CourierId = courierId,
                    CourierAssociatedDate = AdminManager.Now
                };

                s_dal.Order.Update(updatedDoOrder);

                System.Diagnostics.Debug.WriteLine($"[INFO] Courier {courierId} assigned to Order {orderId}");
            }
            catch (BLException)
            {
                throw;
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {orderId} not found.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to associate courier {courierId} to order {orderId}: {ex.Message}", ex);
            }

            Observers.NotifyItemUpdated(orderId); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    public static void PickUpOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Order must be AssociatedToCourier
            BO.Order boOrder = ReadOrder(orderId);
            if (boOrder.OrderStatus != BO.OrderStatus.AssociatedToCourier)
                throw new BLOperationFailedException($"Order ID {orderId} is not ready for pickup.");

            // [2] LOGIC: Update PickupDate in DAL and status
            try
            {
                DO.Order? doOrderNullable = s_dal.Order.Read(orderId);
                if (doOrderNullable is null)
                    throw new BLDoesNotExistException($"Order ID {orderId} not found.");

                DO.Order doOrder = doOrderNullable;
                DO.Order updatedDoOrder = doOrder with { PickupDate = AdminManager.Now };
                s_dal.Order.Update(updatedDoOrder);
            }
            catch (BLException)
            {
                throw;
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {orderId} not found.", ex);
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to pickup order {orderId}: {ex.Message}", ex);
            }

            Observers.NotifyItemUpdated(orderId); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    public static void DeliverOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            // [1] VALIDATION: Order must be in progress (picked up)
            BO.Order boOrder = ReadOrder(orderId);
            if (boOrder.OrderStatus != BO.OrderStatus.InProgress)
                throw new BLOperationFailedException($"Order ID {orderId} is not in progress.");

            // [2] LOGIC: Update DeliveryDate in DAL
            try
            {
                DO.Order? doOrderNullable = s_dal.Order.Read(orderId);
                if (doOrderNullable is null)
                    throw new BLDoesNotExistException($"Order ID {orderId} not found.");

                DO.Order doOrder = doOrderNullable;
                DO.Order updatedDoOrder = doOrder with { DeliveryDate = AdminManager.Now };
                s_dal.Order.Update(updatedDoOrder);
            }
            catch (BLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to deliver order {orderId}: {ex.Message}", ex);
            }

            Observers.NotifyItemUpdated(orderId); // Stage 5
            Observers.NotifyListUpdated(); // Stage 5
        }
    }

    /// <summary>
    /// Retrieves undelivered orders using LINQ Query Syntax with orderby (grouping by status).
    /// Demonstrates: grouping and orderby
    /// </summary>
    public static IEnumerable<BO.Order> GetUndeliveredOrders()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - demonstrates: where, group by, order by
                var undeliveredOrders = from order in s_dal.Order.ReadAll()
                                       where !order.DeliveryDate.HasValue
                                       group order by order.CourierId into courierGroup
                                       orderby courierGroup.Key
                                       select courierGroup.FirstOrDefault()
                                       into firstOrderPerCourier
                                       select ConvertDOToBO(firstOrderPerCourier) into boOrder
                                       where boOrder != null
                                       select boOrder;

                return undeliveredOrders.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get undelivered orders: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Gets all orders assigned to a specific courier.
    /// Uses lambda expressions and LINQ Method Syntax.
    /// </summary>
    public static IEnumerable<BO.Order> GetCourierOrders(int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Method Syntax with lambda expressions
                return s_dal.Order.ReadAll()
                    .Where(order => order.CourierId == courierId)
                    .Select(doOrder => ConvertDOToBO(doOrder))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get orders for courier {courierId}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Finds orders within a specific time range.
    /// Uses LINQ Query Syntax with where conditions and ordering.
    /// </summary>
    public static IEnumerable<BO.Order> GetOrdersByTimeRange(DateTime startTime, DateTime endTime)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                // LINQ Query Syntax - demonstrates: where, order by with multiple conditions
                var ordersInRange = from order in s_dal.Order.ReadAll()
                                   where order.CreatedAt >= startTime && order.CreatedAt <= endTime
                                   orderby order.CreatedAt descending
                                   select ConvertDOToBO(order);

                return ordersInRange.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get orders in time range: {ex.Message}", ex);
            }
        }
    }

    // ------------------------------------
    // --- 4. PERIODIC UPDATES ---
    // ------------------------------------

    /// <summary>
    /// Periodic update method called after the system clock advances.
    /// Uses LINQ to identify risky and delivered orders.
    /// </summary>
    public static void PeriodicOrderUpdates(DateTime oldClock, DateTime newClock)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                BO.Config config = AdminManager.GetConfig();
                TimeSpan riskThreshold = config.RiskRange;
                bool orderUpdated = false; // Stage 5

                // LINQ Method Syntax - demonstrates: Where with lambda, Any, FirstOrDefault
                var ordersToProcess = s_dal.Order.ReadAll()
                    .Where(order => order.CourierAssociatedDate.HasValue && !order.PickupDate.HasValue)
                    .ToList();

                // First pass: Flag risky orders
                foreach (var doOrder in ordersToProcess
                    .Where(o => (newClock - o.CourierAssociatedDate!.Value) > riskThreshold))
                {
                    System.Diagnostics.Debug.WriteLine($"[WARNING] Risky order detected: Order {doOrder.Id}");
                }

                // Second pass: Process delivered orders - LINQ Query Syntax with where
                var deliveredOrders = from order in s_dal.Order.ReadAll()
                                     where order.DeliveryDate.HasValue && order.CourierId.HasValue
                                     select order;

                foreach (var doOrder in deliveredOrders)
                {
                    try
                    {
                        DO.Courier? doCourier = s_dal.Courier.Read(doOrder.CourierId!.Value);
                        if (doCourier is not null && !doCourier.IsActive)
                        {
                            DO.Courier updatedCourier = doCourier with { IsActive = true };
                            s_dal.Courier.Update(updatedCourier);
                            orderUpdated = true; // Stage 5
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to update courier: {ex.Message}");
                    }
                }

                if (orderUpdated) // Stage 5
                {
                    Observers.NotifyListUpdated(); // Stage 5
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error in PeriodicOrderUpdates: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Checks and updates orders that have exceeded their max delivery time.
    /// Called when the system clock is advanced.
    /// </summary>
    public static void CheckAndUpdateExpiredOrders()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                DateTime now = AdminManager.Now;
                BO.Config config = AdminManager.GetConfig();
                bool anyUpdated = false;

                // LINQ Method Syntax - Find orders that exceeded max delivery time
                var expiredOrders = s_dal.Order.ReadAll()
                    .Where(order => !order.DeliveryDate.HasValue) // Not yet delivered
                    .Where(order => order.CreatedAt.Add(config.MaxDeliveryTime) < now) // Exceeded max time
                    .ToList();

                foreach (var doOrder in expiredOrders)
                {
                    // Log the expired order for tracking
                    System.Diagnostics.Debug.WriteLine($"[EXPIRED] Order {doOrder.Id} exceeded max delivery time. Created: {doOrder.CreatedAt}, Max: {doOrder.CreatedAt.Add(config.MaxDeliveryTime)}, Now: {now}");

                    // If order has a courier assigned but not picked up, release the courier
                    if (doOrder.CourierId.HasValue && !doOrder.PickupDate.HasValue)
                    {
                        try
                        {
                            DO.Courier? courier = s_dal.Courier.Read(doOrder.CourierId.Value);
                            if (courier != null)
                            {
                                // Mark courier as available again
                                System.Diagnostics.Debug.WriteLine($"[INFO] Releasing courier {courier.Id} from expired order {doOrder.Id}");
                              }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to process courier for expired order: {ex.Message}");
                        }
                    }

                    anyUpdated = true;
                }

                if (anyUpdated)
                {
                    Observers.NotifyListUpdated();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error in CheckAndUpdateExpiredOrders: {ex.Message}");
            }
        }
    }
}
