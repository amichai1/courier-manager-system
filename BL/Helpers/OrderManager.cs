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
        BO.Courier? assignedCourier = null;
        if (doOrder.CourierId.HasValue)
        {
            try
            {
                assignedCourier = CourierManager.ReadCourier(doOrder.CourierId.Value);
            }
            catch
            {
                assignedCourier = null;
            }
        }

        // Determine OrderStatus based on dates and courier assignment
        BO.OrderStatus orderStatus = BO.OrderStatus.Open;
        if (doOrder.DeliveryDate.HasValue)
        {
            orderStatus = BO.OrderStatus.Delivered;
        }
        else if (doOrder.PickupDate.HasValue || doOrder.CourierAssociatedDate.HasValue)
        {
            orderStatus = BO.OrderStatus.InProgress;
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
    // --- 1.5. HELPER METHODS ---
    // ------------------------------------

    /// <summary>
    /// Retrieves delivery history for a specific order (internal use).
    /// Shows all deliveries - both completed and in progress.
    /// </summary>
    private static IEnumerable<BO.DeliveryPerOrderInList> GetDeliveryHistoryForOrder(int orderId)
    {
        try
        {
            // Get all deliveries for this order
            var allDeliveries = s_dal.Delivery.ReadAll()
                .Where(d => d.OrderId == orderId)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[GetDeliveryHistoryForOrder] Order {orderId}: Found {allDeliveries.Count} deliveries in DAL");

            if (!allDeliveries.Any())
            {
                return new List<BO.DeliveryPerOrderInList>();
            }

            // Get order info once
            var orderInfo = s_dal.Order.Read(orderId);

            // Build delivery history list
            var deliveryHistory = new List<BO.DeliveryPerOrderInList>();

            foreach (var delivery in allDeliveries)
            {
                DO.Courier? courierInfo = null;
                try
                {
                    if (delivery.CourierId > 0)
                    {
                        courierInfo = s_dal.Courier.Read(delivery.CourierId);
                    }
                }
                catch
                {
                    // Courier might have been deleted
                }

                var historyItem = new BO.DeliveryPerOrderInList
                {
                    DeliveryId = delivery.Id,
                    CourierId = delivery.CourierId,
                    CourierName = courierInfo?.Name ?? "Unknown",
                    DeliveryType = (BO.DeliveryType)(courierInfo?.DeliveryType ?? DO.DeliveryType.Car),
                    StartTimeDelivery = delivery.StartTime,
                    EndType = (BO.DeliveryStatus?)delivery.CompletionStatus,
                    EndTime = delivery.EndTime,
                    DeliveryAddress = orderInfo?.Address ?? "Unknown"
                };

                deliveryHistory.Add(historyItem);
            }

            System.Diagnostics.Debug.WriteLine($"[GetDeliveryHistoryForOrder] Order {orderId}: Returning {deliveryHistory.Count} history items");

            return deliveryHistory;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetDeliveryHistoryForOrder] ERROR for Order {orderId}: {ex.Message}");
            return new List<BO.DeliveryPerOrderInList>();
        }
    }

    /// <summary>
    /// Public method to get delivery history for a specific order.
    /// </summary>
    public static IEnumerable<BO.DeliveryPerOrderInList> GetDeliveryHistoryForOrderPublic(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            return GetDeliveryHistoryForOrder(orderId);
        }
    }

    private static BO.ScheduleStatus CalculateScheduleStatus(DO.Order doOrder)
    {
        try
        {
            var config = AdminManager.GetConfig();
            DateTime nowTime = AdminManager.Now;

            // For DELIVERED orders - check if delivered on time
            if (doOrder.DeliveryDate.HasValue)
            {
                TimeSpan deliveryTime = doOrder.DeliveryDate.Value - doOrder.CreatedAt;
                
                if (deliveryTime <= config.MaxDeliveryTime - config.RiskRange)
                {
                    // Delivered well within time (before risk threshold)
                    return BO.ScheduleStatus.OnTime;
                }
                else if (deliveryTime <= config.MaxDeliveryTime)
                {
                    // Delivered on time but was cutting it close (in risk zone)
                    return BO.ScheduleStatus.InRisk;
                }
                else
                {
                    // Delivered after max time
                    return BO.ScheduleStatus.Late;
                }
            }

            // For OPEN/IN-PROGRESS orders - check remaining time
            DateTime maxDeliveryDeadline = doOrder.CreatedAt.Add(config.MaxDeliveryTime);
            TimeSpan timeUntilMax = maxDeliveryDeadline - nowTime;

            if (timeUntilMax <= TimeSpan.Zero)
            {
                return BO.ScheduleStatus.Late;
            }
            
            if (timeUntilMax <= config.RiskRange)
            {
                return BO.ScheduleStatus.InRisk;
            }

            return BO.ScheduleStatus.OnTime;
        }
        catch
        {
            return BO.ScheduleStatus.OnTime;
        }
    }

    private static DateTime CalculateExpectedDeliveryTime(DO.Order doOrder)
    {
        try
        {
            var config = AdminManager.GetConfig();
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
            if (order.Weight <= 0 || string.IsNullOrWhiteSpace(order.CustomerName))
                throw new BLInvalidValueException("Order Weight or Customer Name is missing or invalid.");

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

            Observers.NotifyListUpdated();
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
    /// LINQ Method Syntax - demonstrates: Select, Where, ToList
    /// </summary>
    public static IEnumerable<BO.Order> ReadAllOrders(Func<BO.Order, bool>? filter = null)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
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
            if (order.OrderStatus != BO.OrderStatus.Open)
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

            Observers.NotifyItemUpdated(order.Id);
            Observers.NotifyListUpdated();
        }
    }

    public static void DeleteOrder(int id)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                BO.Order boOrder = ReadOrder(id);
                if (boOrder.OrderStatus != BO.OrderStatus.Open)
                    throw new BLOperationFailedException($"Cannot delete Order ID {id}: It has already been processed or is active.");

                s_dal.Order.Delete(id);
            }
            catch (DO.DalDoesNotExistException ex)
            {
                throw new BLDoesNotExistException($"Order ID {id} not found for deletion.", ex);
            }

            Observers.NotifyItemUpdated(id);
            Observers.NotifyListUpdated();
        }
    }

    // ------------------------------------
    // --- 3. SPECIFIC OPERATIONS ---
    // ------------------------------------

    public static void AssociateCourierToOrder(int orderId, int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            BO.Order boOrder = ReadOrder(orderId);
            BO.Courier boCourier = CourierManager.ReadCourier(courierId);

            if (boOrder.OrderStatus != BO.OrderStatus.Open)
                throw new BLOperationFailedException($"Order ID {orderId} is not open (Status: {boOrder.OrderStatus}).");
            if (boCourier.Status != BO.CourierStatus.Available)
                throw new BLOperationFailedException($"Courier ID {courierId} is not available (Status: {boCourier.Status}).");

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

            Observers.NotifyItemUpdated(orderId);
            Observers.NotifyListUpdated();
            CourierManager.Observers.NotifyItemUpdated(courierId);
            CourierManager.Observers.NotifyListUpdated();
        }
    }

    public static void PickUpOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            DO.Order? doOrderCheck = s_dal.Order.Read(orderId);
            if (doOrderCheck is null)
                throw new BLDoesNotExistException($"Order ID {orderId} not found.");

            if (!doOrderCheck.CourierAssociatedDate.HasValue || doOrderCheck.PickupDate.HasValue)
                throw new BLOperationFailedException($"Order ID {orderId} is not ready for pickup.");

            try
            {
                DO.Order updatedDoOrder = doOrderCheck with { PickupDate = AdminManager.Now };
                s_dal.Order.Update(updatedDoOrder);
            }
            catch (BLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to pickup order {orderId}: {ex.Message}", ex);
            }

            Observers.NotifyItemUpdated(orderId);
            Observers.NotifyListUpdated();
        }
    }

    public static void DeliverOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            DO.Order? doOrderCheck = s_dal.Order.Read(orderId);
            if (doOrderCheck is null)
                throw new BLDoesNotExistException($"Order ID {orderId} not found.");

            if (!doOrderCheck.PickupDate.HasValue)
                throw new BLOperationFailedException($"Order ID {orderId} has not been picked up yet.");

            if (doOrderCheck.DeliveryDate.HasValue)
                throw new BLOperationFailedException($"Order ID {orderId} is already delivered.");

            try
            {
                DO.Order updatedDoOrder = doOrderCheck with { DeliveryDate = AdminManager.Now };
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

            Observers.NotifyItemUpdated(orderId);
            Observers.NotifyListUpdated();
        }
    }

    /// <summary>
    /// Cancels an order. If the order is in progress, sends email notification to courier.
    /// </summary>
    public static void CancelOrder(int orderId)
    {
        lock (AdminManager.BlMutex)
        {
            BO.Order boOrder = ReadOrder(orderId);

            if (boOrder.OrderStatus == BO.OrderStatus.Delivered)
                throw new BLOperationFailedException($"Cannot cancel Order ID {orderId}: Order has already been delivered.");

            if (boOrder.OrderStatus == BO.OrderStatus.Canceled)
                throw new BLOperationFailedException($"Order ID {orderId} is already canceled.");

            bool wasInProgress = boOrder.OrderStatus == BO.OrderStatus.InProgress && boOrder.CourierId.HasValue;
            BO.Courier? courier = null;

            if (wasInProgress)
            {
                try
                {
                    courier = CourierManager.ReadCourier(boOrder.CourierId!.Value);
                }
                catch
                {
                    // Courier might have been deleted
                }
            }

            try
            {
                DO.Order? doOrderNullable = s_dal.Order.Read(orderId);
                if (doOrderNullable is null)
                    throw new BLDoesNotExistException($"Order ID {orderId} not found.");

                // Create a canceled delivery record if order was in progress
                if (wasInProgress)
                {
                    try
                    {
                        DO.Delivery canceledDelivery = new DO.Delivery(
                            Id: 0,
                            OrderId: orderId,
                            CourierId: boOrder.CourierId!.Value,
                            DeliveryType: courier?.DeliveryType != null
                                ? (DO.DeliveryType)courier.DeliveryType
                                : DO.DeliveryType.Car,
                            StartTime: boOrder.CourierAssociatedDate ?? AdminManager.Now,
                            ActualDistance: 0
                        )
                        {
                            CompletionStatus = DO.DeliveryStatus.Cancelled,
                            EndTime = AdminManager.Now
                        };

                        s_dal.Delivery.Create(canceledDelivery);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WARNING] Failed to create canceled delivery record: {ex.Message}");
                    }
                }

                // Clear courier info from the order
                DO.Order doOrder = doOrderNullable;
                DO.Order updatedDoOrder = doOrder with
                {
                    CourierId = null,
                    CourierAssociatedDate = null,
                    PickupDate = null,
                    DeliveryDate = null
                };

                s_dal.Order.Update(updatedDoOrder);

                // Send email to courier if order was in progress
                if (wasInProgress && courier != null && !string.IsNullOrEmpty(courier.Email))
                {
                    EmailHelper.SendOrderCancellationEmail(
                        courier.Email,
                        courier.Name,
                        orderId,
                        boOrder.CustomerName ?? "Unknown Customer",
                        boOrder.Address);
                }

                System.Diagnostics.Debug.WriteLine($"[INFO] Order {orderId} canceled successfully. Was in progress: {wasInProgress}");
            }
            catch (BLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to cancel order {orderId}: {ex.Message}", ex);
            }

            Observers.NotifyItemUpdated(orderId);
            Observers.NotifyListUpdated();

            if (wasInProgress && boOrder.CourierId.HasValue)
            {
                CourierManager.Observers.NotifyItemUpdated(boOrder.CourierId.Value);
                CourierManager.Observers.NotifyListUpdated();
            }
        }
    }

    /// <summary>
    /// LINQ Query Syntax - demonstrates: from, where, group by, orderby, select
    /// </summary>
    public static IEnumerable<BO.Order> GetUndeliveredOrders()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
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
    /// LINQ Method Syntax - demonstrates: Where, Select, ToList with lambda
    /// </summary>
    public static IEnumerable<BO.Order> GetCourierOrders(int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
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
    /// LINQ Query Syntax - demonstrates: from, where, orderby, select
    /// </summary>
    public static IEnumerable<BO.Order> GetOrdersByTimeRange(DateTime startTime, DateTime endTime)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
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

    /// <summary>
    /// LINQ Method Syntax - demonstrates: Where, OrderBy with lambda
    /// </summary>
    public static IEnumerable<BO.Order> GetAvailableOrdersForCourier(int courierId)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                BO.Courier courier = CourierManager.ReadCourier(courierId);

                var availableOrders = s_dal.Order.ReadAll()
                    .Where(o => !o.CourierId.HasValue)
                    .Where(o => !o.CourierAssociatedDate.HasValue)
                    .ToList();

                var withinDistance = new List<BO.Order>();

                foreach (var doOrder in availableOrders)
                {
                    double distance = BO.Order.CalculateAirDistance(
                        courier.Location.Latitude,
                        courier.Location.Longitude,
                        doOrder.Latitude,
                        doOrder.Longitude);

                    if (courier.MaxDeliveryDistance == null || distance <= courier.MaxDeliveryDistance.Value)
                    {
                        BO.Order boOrder = ConvertDOToBO(doOrder);
                        boOrder.ArialDistance = distance;
                        withinDistance.Add(boOrder);
                    }
                }

                return withinDistance.OrderBy(o => o.ArialDistance).ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException(
                    $"Failed to get available orders for courier {courierId}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// LINQ Method Syntax - demonstrates: GroupBy, ToDictionary, GetValueOrDefault
    /// </summary>
    public static BO.OrderStatusSummary GetOrderStatusSummary()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                var allOrders = ReadAllOrders();

                var statusGroups = allOrders
                    .GroupBy(o => o.OrderStatus)
                    .ToDictionary(g => g.Key, g => g.Count());

                var scheduleGroups = allOrders
                    .Where(o => o.OrderStatus == BO.OrderStatus.Open || o.OrderStatus == BO.OrderStatus.InProgress)
                    .GroupBy(o => o.ScheduleStatus)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new BO.OrderStatusSummary
                {
                    OpenCount = statusGroups.GetValueOrDefault(BO.OrderStatus.Open, 0),
                    InProgressCount = statusGroups.GetValueOrDefault(BO.OrderStatus.InProgress, 0),
                    DeliveredCount = statusGroups.GetValueOrDefault(BO.OrderStatus.Delivered, 0),
                    OrderRefusedCount = statusGroups.GetValueOrDefault(BO.OrderStatus.OrderRefused, 0),
                    CanceledCount = statusGroups.GetValueOrDefault(BO.OrderStatus.Canceled, 0),
                    OnTimeCount = scheduleGroups.GetValueOrDefault(BO.ScheduleStatus.OnTime, 0),
                    InRiskCount = scheduleGroups.GetValueOrDefault(BO.ScheduleStatus.InRisk, 0),
                    LateCount = scheduleGroups.GetValueOrDefault(BO.ScheduleStatus.Late, 0)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error in GetOrderStatusSummary: {ex.Message}");
                return new BO.OrderStatusSummary();
            }
        }
    }

    // ------------------------------------
    // --- 4. PERIODIC UPDATES ---
    // ------------------------------------

    public static void PeriodicOrderUpdates(DateTime oldClock, DateTime newClock)
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                BO.Config config = AdminManager.GetConfig();
                TimeSpan riskThreshold = config.RiskRange;
                bool orderUpdated = false;

                // LINQ Method Syntax - demonstrates: Where with lambda
                var ordersToProcess = s_dal.Order.ReadAll()
                    .Where(order => order.CourierAssociatedDate.HasValue && !order.PickupDate.HasValue)
                    .ToList();

                foreach (var doOrder in ordersToProcess
                    .Where(o => (newClock - o.CourierAssociatedDate!.Value) > riskThreshold))
                {
                    System.Diagnostics.Debug.WriteLine($"[WARNING] Risky order detected: Order {doOrder.Id}");
                }

                // LINQ Query Syntax - demonstrates: from, where, select
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
                            orderUpdated = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to update courier: {ex.Message}");
                    }
                }

                if (orderUpdated)
                {
                    Observers.NotifyListUpdated();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error in PeriodicOrderUpdates: {ex.Message}");
            }
        }
    }

    public static void CheckAndUpdateExpiredOrders()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                DateTime now = AdminManager.Now;
                BO.Config config = AdminManager.GetConfig();
                bool anyUpdated = false;

                // LINQ Method Syntax - demonstrates: Where with multiple conditions
                var expiredOrders = s_dal.Order.ReadAll()
                    .Where(order => !order.DeliveryDate.HasValue)
                    .Where(order => order.CreatedAt.Add(config.MaxDeliveryTime) < now)
                    .ToList();

                foreach (var doOrder in expiredOrders)
                {
                    System.Diagnostics.Debug.WriteLine($"[EXPIRED] Order {doOrder.Id} exceeded max delivery time.");

                    if (doOrder.CourierId.HasValue && !doOrder.PickupDate.HasValue)
                    {
                        try
                        {
                            DO.Courier? courier = s_dal.Courier.Read(doOrder.CourierId.Value);
                            if (courier != null)
                            {
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

    /// <summary>
    /// Gets all orders as a lightweight list for display purposes.
    /// LINQ Query Syntax - demonstrates: from, let, select
    /// </summary>
    public static IEnumerable<BO.OrderInList> GetOrderList()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                var orderList = from doOrder in s_dal.Order.ReadAll()
                                let courier = doOrder.CourierId.HasValue
                                    ? s_dal.Courier.Read(doOrder.CourierId.Value)
                                    : null
                                let deliveryHistory = GetDeliveryHistoryForOrder(doOrder.Id)
                                let currentDelivery = deliveryHistory.FirstOrDefault(d => d.EndTime == null)
                                let airDistance = CalculateAirDistance(doOrder)
                                let orderStatus = doOrder.DeliveryDate.HasValue
                                    ? BO.OrderStatus.Delivered
                                    : (doOrder.PickupDate.HasValue || doOrder.CourierAssociatedDate.HasValue)
                                        ? BO.OrderStatus.InProgress
                                        : BO.OrderStatus.Open
                                let completionTime = doOrder.DeliveryDate.HasValue
                                    ? doOrder.DeliveryDate.Value - doOrder.CreatedAt
                                    : TimeSpan.Zero
                                let handlingTime = doOrder.CourierAssociatedDate.HasValue
                                    ? (doOrder.DeliveryDate ?? AdminManager.Now) - doOrder.CourierAssociatedDate.Value
                                    : TimeSpan.Zero
                                select new BO.OrderInList
                                {
                                    OrderId = doOrder.Id,
                                    DeliveryId = currentDelivery?.DeliveryId,
                                    OrderType = (BO.OrderType)doOrder.OrderType,
                                    Distance = airDistance,
                                    OrderStatus = orderStatus,
                                    ScheduleStatus = CalculateScheduleStatus(doOrder),
                                    OrderCompletionTime = completionTime,
                                    HandlingTime = handlingTime,
                                    TotalDeliveries = deliveryHistory.Count(),
                                    CustomerName = doOrder.CustomerName,
                                    CustomerPhone = doOrder.CustomerPhone,
                                    Address = doOrder.Address,
                                    CourierName = courier?.Name,
                                    CreatedAt = doOrder.CreatedAt
                                };

                return orderList.ToList();
            }
            catch (Exception ex)
            {
                throw new BLOperationFailedException($"Failed to get order list: {ex.Message}", ex);
            }
        }
    }
}
