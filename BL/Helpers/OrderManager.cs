using BO;
using DalApi;
using DO;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BL.Helpers;

/// <summary>
/// Stage 7 - Business Logic Manager for Orders
/// Handles all order-related operations with async network requests (Geocoding, Email).
/// Uses async/await all the way to keep UI responsive.
/// </summary>
internal static class OrderManager
{
    private static readonly IDal s_dal = DalApi.Factory.Get;
    internal static ObserverManager Observers = new();
    private static readonly AsyncMutex s_periodicMutex = new(); //stage 7
    private static readonly AsyncMutex s_simulationMutex = new(); //stage 7

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
            try { assignedCourier = CourierManager.ReadCourier(doOrder.CourierId.Value); }
            catch { assignedCourier = null; }
        }

        // Fetch delivery history
        IEnumerable<BO.DeliveryPerOrderInList> deliveryHistory = GetDeliveryHistoryForOrder(doOrder.Id);

        // Status Logic
        BO.OrderStatus orderStatus = BO.OrderStatus.Open;
        if (doOrder.DeliveryDate.HasValue)
        {
            var lastDelivery = deliveryHistory.Where(d => d.EndTime != null).OrderByDescending(d => d.EndTime).FirstOrDefault();
            if (lastDelivery != null && lastDelivery.EndType == BO.DeliveryStatus.CustomerRefused)
                orderStatus = BO.OrderStatus.OrderRefused;
            else
                orderStatus = BO.OrderStatus.Delivered;
        }
        else if (doOrder.PickupDate.HasValue || doOrder.CourierAssociatedDate.HasValue)
        {
            orderStatus = BO.OrderStatus.InProgress;
        }

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
            ExpectedDeliverdTime = doOrder.CreatedAt.AddHours(2),
            MaxDeliveredTime = doOrder.CreatedAt.Add(AdminManager.GetConfig().MaxDeliveryTime),
            CourierId = doOrder.CourierId,
            CourierName = assignedCourier?.Name,
            CourierAssociatedDate = doOrder.CourierAssociatedDate,
            PickupDate = doOrder.PickupDate,
            DeliveryDate = doOrder.DeliveryDate,
            OrderComplitionTime = doOrder.DeliveryDate.HasValue && doOrder.CreatedAt != default ? doOrder.DeliveryDate.Value - doOrder.CreatedAt : null,
            DeliveryHistory = deliveryHistory.ToList(),
            CustomerLocation = new BO.Location { Latitude = doOrder.Latitude, Longitude = doOrder.Longitude },
            ArialDistance = CalculateAirDistance(doOrder) // Uses BO helper internally
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
    // --- HELPERS ---
    // ------------------------------------

    /// <summary>
    /// Uses the BO static function for logic, ensuring we don't duplicate calculation rules in BL.
    /// </summary>
    private static double CalculateAirDistance(DO.Order doOrder)
    {
        var config = AdminManager.GetConfig();
        if (config.CompanyLatitude is null || config.CompanyLongitude is null)
            return 0;

        return BO.Order.CalculateAirDistance(
            config.CompanyLatitude.Value, config.CompanyLongitude.Value,
            doOrder.Latitude, doOrder.Longitude);
    }

    private static BO.ScheduleStatus CalculateScheduleStatus(DO.Order doOrder)
    {
        try
        {
            var config = AdminManager.GetConfig();
            DateTime nowTime = AdminManager.Now;

            if (doOrder.DeliveryDate.HasValue)
            {
                TimeSpan deliveryTime = doOrder.DeliveryDate.Value - doOrder.CreatedAt;
                
                if (deliveryTime <= config.MaxDeliveryTime - config.RiskRange) return BO.ScheduleStatus.OnTime;
                else if (deliveryTime <= config.MaxDeliveryTime) return BO.ScheduleStatus.InRisk;
                else return BO.ScheduleStatus.Late;
            }

            DateTime maxDeliveryDeadline = doOrder.CreatedAt.Add(config.MaxDeliveryTime);
            TimeSpan timeUntilMax = maxDeliveryDeadline - nowTime;

            if (timeUntilMax <= TimeSpan.Zero) return BO.ScheduleStatus.Late;
            if (timeUntilMax <= config.RiskRange) return BO.ScheduleStatus.InRisk;
            return BO.ScheduleStatus.OnTime;
        }
        catch { return BO.ScheduleStatus.OnTime; }
    }

    private static IEnumerable<BO.DeliveryPerOrderInList> GetDeliveryHistoryForOrder(int orderId)
    {
        try
        {
            var allDeliveries = s_dal.Delivery.ReadAll().Where(d => d.OrderId == orderId).OrderBy(d => d.StartTime).ToList();
            if (!allDeliveries.Any()) return new List<BO.DeliveryPerOrderInList>();

            var orderInfo = s_dal.Order.Read(orderId);
            var deliveryHistory = new List<BO.DeliveryPerOrderInList>();

            foreach (var delivery in allDeliveries)
            {
                string cName = "Unknown";
                DO.DeliveryType dType = DO.DeliveryType.Car;
                try
                {
                    if (delivery.CourierId > 0)
                    {
                        var c = s_dal.Courier.Read(delivery.CourierId);
                        if (c != null) { cName = c.Name; dType = c.DeliveryType; }
                    }
                }
                catch { }

                deliveryHistory.Add(new BO.DeliveryPerOrderInList
                {
                    DeliveryId = delivery.Id,
                    CourierId = delivery.CourierId,
                    CourierName = cName,
                    DeliveryType = (BO.DeliveryType)dType,
                    StartTimeDelivery = delivery.StartTime,
                    EndType = (BO.DeliveryStatus?)delivery.CompletionStatus,
                    EndTime = delivery.EndTime,
                    DeliveryAddress = orderInfo?.Address ?? "Unknown"
                });
            }
            return deliveryHistory;
        }
        catch { return new List<BO.DeliveryPerOrderInList>(); }
    }

    // --- CRUD OPERATIONS ---

    public static void CreateOrder(BO.Order order)
    {
        lock (AdminManager.BlMutex)
        {
            DO.Order doOrder = ConvertBOToDO(order);
            s_dal.Order.Create(doOrder);
        }
        Observers.NotifyListUpdated();
    }

    public static BO.Order ReadOrder(int id)
    {
        lock (AdminManager.BlMutex) //stage 7
            return ConvertDOToBO(s_dal.Order.Read(id)!);
    }

    public static IEnumerable<BO.Order> ReadAllOrders(Func<BO.Order, bool>? f = null)
    {
        lock (AdminManager.BlMutex) //stage 7
        {
            var L = s_dal.Order.ReadAll()
                .Select(d =>
                {
                    try { return ConvertDOToBO(d); }
                    catch { return null; }
                })
                .Where(x => x != null)
                .Cast<BO.Order>()
                .ToList();
            return f != null ? L.Where(f).ToList() : L;
        }
    }

    public static void UpdateOrder(BO.Order order)
    {
        lock (AdminManager.BlMutex) //stage 7
            s_dal.Order.Update(ConvertBOToDO(order));
        
        Observers.NotifyItemUpdated(order.Id);
        Observers.NotifyListUpdated();
    }

    public static void DeleteOrder(int id)
    {
        lock (AdminManager.BlMutex) //stage 7
            s_dal.Order.Delete(id);
        
        Observers.NotifyItemUpdated(id);
        Observers.NotifyListUpdated();
    }

    // --- ACTIONS ---
    public static void AssociateCourierToOrder(int orderId, int courierId)
    {
        lock (AdminManager.BlMutex) //stage 7
        {
            DO.Order d = s_dal.Order.Read(orderId)!;
            var c = s_dal.Courier.Read(courierId)!;
            s_dal.Order.Update(d with { CourierId = courierId, CourierAssociatedDate = AdminManager.Now });
            s_dal.Delivery.Create(new DO.Delivery(0, orderId, courierId, (DO.DeliveryType)c.DeliveryType, AdminManager.Now, 0) 
            { 
                CompletionStatus = null, 
                EndTime = null 
            });
        }
        
        Observers.NotifyItemUpdated(orderId);
        Observers.NotifyListUpdated();
        CourierManager.Observers.NotifyItemUpdated(courierId);
        CourierManager.Observers.NotifyListUpdated();
    }

    public static void PickUpOrder(int orderId)
    {
        lock (AdminManager.BlMutex) //stage 7
        {
            DO.Order d = s_dal.Order.Read(orderId)!;
            s_dal.Order.Update(d with { PickupDate = AdminManager.Now });
        }
        
        Observers.NotifyItemUpdated(orderId);
        Observers.NotifyListUpdated();
    }

    public static void DeliverOrder(int orderId)
    {
        lock (AdminManager.BlMutex) //stage 7
        {
            DO.Order d = s_dal.Order.Read(orderId)!;
            s_dal.Order.Update(d with { DeliveryDate = AdminManager.Now });
            var ad = s_dal.Delivery.ReadAll(x => x.OrderId == orderId && x.EndTime == null).FirstOrDefault();
            if (ad != null)
                s_dal.Delivery.Update(ad with { CompletionStatus = DO.DeliveryStatus.Completed, EndTime = AdminManager.Now });
        }
        
        Observers.NotifyItemUpdated(orderId);
        Observers.NotifyListUpdated();
    }

    public static void RefuseOrder(int orderId)
    {
        lock (AdminManager.BlMutex) //stage 7
        {
            DO.Order d = s_dal.Order.Read(orderId)!;
            var ad = s_dal.Delivery.ReadAll(x => x.OrderId == orderId && x.EndTime == null).FirstOrDefault();
            if (ad != null)
                s_dal.Delivery.Update(ad with { CompletionStatus = DO.DeliveryStatus.CustomerRefused, EndTime = AdminManager.Now });

            if (d.OrderType == DO.OrderType.RestaurantFood)
                s_dal.Order.Update(d with { DeliveryDate = AdminManager.Now });
            else
                s_dal.Order.Update(d with { CourierId = null, CourierAssociatedDate = null, PickupDate = null, DeliveryDate = null });
        }
        
        Observers.NotifyItemUpdated(orderId);
        Observers.NotifyListUpdated();
    }

    public static void CancelOrder(int orderId)
    {
        lock (AdminManager.BlMutex) //stage 7
        {
            BO.Order b = ReadOrder(orderId);
            bool ip = b.OrderStatus == BO.OrderStatus.InProgress;
            if (ip)
            {
                var ad = s_dal.Delivery.ReadAll(x => x.OrderId == orderId && x.EndTime == null).FirstOrDefault();
                if (ad != null)
                    s_dal.Delivery.Update(ad with { CompletionStatus = DO.DeliveryStatus.Cancelled, EndTime = AdminManager.Now });
            }
            s_dal.Order.Update(s_dal.Order.Read(orderId)! with 
            { 
                CourierId = null, 
                CourierAssociatedDate = null, 
                PickupDate = null, 
                DeliveryDate = null 
            });
        }
        
        Observers.NotifyItemUpdated(orderId);
        Observers.NotifyListUpdated();
    }

    public static IEnumerable<BO.OrderInList> GetOrderList()
    {
        lock (AdminManager.BlMutex) //stage 7
        {
            var allOrders = s_dal.Order.ReadAll().ToList(); // Convert to concrete list
            var allDeliveries = s_dal.Delivery.ReadAll().ToList();
            var allCouriers = s_dal.Courier.ReadAll().ToDictionary(c => c.Id);
            var deliveryLookup = allDeliveries.ToLookup(d => d.OrderId);
            var list = new List<BO.OrderInList>();

            foreach (var doOrder in allOrders)
            {
                var currentHist = deliveryLookup[doOrder.Id].ToList();
                BO.OrderStatus status = BO.OrderStatus.Open;
                if (doOrder.DeliveryDate.HasValue)
                {
                    var last = currentHist.Where(x => x.EndTime != null).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (last != null && last.CompletionStatus == DO.DeliveryStatus.CustomerRefused)
                        status = BO.OrderStatus.OrderRefused;
                    else
                        status = BO.OrderStatus.Delivered;
                }
                else if (doOrder.PickupDate.HasValue || doOrder.CourierAssociatedDate.HasValue)
                    status = BO.OrderStatus.InProgress;

                string? cName = (doOrder.CourierId.HasValue && allCouriers.ContainsKey(doOrder.CourierId.Value))
                    ? allCouriers[doOrder.CourierId.Value].Name
                    : null;

                var currentDelivery = currentHist.Where(x => x.EndTime == null).FirstOrDefault();

                list.Add(new BO.OrderInList
                {
                    OrderId = doOrder.Id,
                    DeliveryId = currentDelivery?.Id,
                    OrderType = (BO.OrderType)doOrder.OrderType,
                    Distance = CalculateAirDistance(doOrder),
                    OrderStatus = status,
                    ScheduleStatus = CalculateScheduleStatus(doOrder),
                    OrderCompletionTime = doOrder.DeliveryDate.HasValue ? doOrder.DeliveryDate.Value - doOrder.CreatedAt : TimeSpan.Zero,
                    TotalDeliveries = currentHist.Count,
                    CustomerName = doOrder.CustomerName,
                    CourierName = cName,
                    CreatedAt = doOrder.CreatedAt,
                    CustomerPhone = doOrder.CustomerPhone,
                    Address = doOrder.Address
                });
            }
            return list;
        }
    }

    public static IEnumerable<BO.Order> GetAvailableOrdersForCourier(int courierId)
    {
        lock (AdminManager.BlMutex) //stage 7
            return s_dal.Order.ReadAll(o => !o.CourierId.HasValue).Select(ConvertDOToBO).ToList();
    }

    // Wrappers
    public static IEnumerable<BO.DeliveryPerOrderInList> GetDeliveryHistoryForOrderPublic(int id)
    {
        lock (AdminManager.BlMutex) //stage 7
            return GetDeliveryHistoryForOrder(id);
    }

    public static BO.OrderStatusSummary GetOrderStatusSummary()
    {
        lock (AdminManager.BlMutex) //stage 7
        {
            var all = ReadAllOrders();
            return new BO.OrderStatusSummary
            {
                OpenCount = all.Count(o => o.OrderStatus == BO.OrderStatus.Open),
                InProgressCount = all.Count(o => o.OrderStatus == BO.OrderStatus.InProgress),
                DeliveredCount = all.Count(o => o.OrderStatus == BO.OrderStatus.Delivered),
                OrderRefusedCount = all.Count(o => o.OrderStatus == BO.OrderStatus.OrderRefused),
                CanceledCount = all.Count(o => o.OrderStatus == BO.OrderStatus.Canceled),
                OnTimeCount = all.Count(o => o.ScheduleStatus == BO.ScheduleStatus.OnTime),
                InRiskCount = all.Count(o => o.ScheduleStatus == BO.ScheduleStatus.InRisk),
                LateCount = all.Count(o => o.ScheduleStatus == BO.ScheduleStatus.Late)
            };
        }
    }

    public static IEnumerable<BO.Order> GetCourierOrders(int courierId)
    {
        lock (AdminManager.BlMutex) //stage 7
            return s_dal.Order.ReadAll(o => o.CourierId == courierId).Select(ConvertDOToBO).ToList();
    }

    // --- PERIODIC UPDATES (Stage 7) ---

    internal static void PeriodicOrderUpdates(DateTime oldClock, DateTime newClock)
    {
        if (s_periodicMutex.CheckAndSetInProgress())
            return;

        try
        {
            // Logic here if needed for order periodic updates
        }
        finally
        {
            s_periodicMutex.UnsetInProgress();
        }
    }

    internal static void CheckAndUpdateExpiredOrders()
    {
        lock (AdminManager.BlMutex)
        {
            try
            {
                var config = AdminManager.GetConfig();
                
                var inProgressOrders = s_dal.Order.ReadAll()
                    .Where(o => !o.DeliveryDate.HasValue && o.CreatedAt != default)
                    .ToList();

                var now = AdminManager.Now;
                bool anyUpdated = false;

                foreach (var order in inProgressOrders)
                {
                    DateTime maxDeadline = order.CreatedAt.Add(config.MaxDeliveryTime);
                    
                    if (now > maxDeadline)
                    {
                        s_dal.Order.Update(order with { DeliveryDate = now });
                        
                        var delivery = s_dal.Delivery.ReadAll(d => d.OrderId == order.Id && !d.EndTime.HasValue)
                            .FirstOrDefault();
                        
                        if (delivery != null)
                        {
                            s_dal.Delivery.Update(delivery with 
                            { 
                                CompletionStatus = DO.DeliveryStatus.Failed, 
                                EndTime = now 
                            });
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[SIMULATOR] Order {order.Id} marked as expired/failed");
                        Observers.NotifyItemUpdated(order.Id);
                        anyUpdated = true;
                    }
                }

                if (anyUpdated)
                    Observers.NotifyListUpdated();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] CheckAndUpdateExpiredOrders: {ex.Message}");
            }
        }
    }

    internal static async Task PeriodicOrderUpdatesAsync(DateTime oldClock, DateTime newClock)
    {
        if (s_periodicMutex.CheckAndSetInProgress())
            return;

        try
        {
            // Logic here if needed for order periodic updates
            await Task.CompletedTask;
        }
        finally
        {
            s_periodicMutex.UnsetInProgress();
        }
    }

    // --- SIMULATION (Stage 7) ---
    internal static async Task SimulateOrdersAsync() //stage 7
    {
        if (s_simulationMutex.CheckAndSetInProgress())
            return;

        try
        {
            // Simulation logic: Auto-complete orders that are overdue
            List<int> ordersToNotify = new();
            
            lock (AdminManager.BlMutex)
            {
                var allOrders = s_dal.Order.ReadAll()
                    .Where(o => o.CourierAssociatedDate.HasValue && !o.DeliveryDate.HasValue)
                    .ToList();
                
                foreach (var order in allOrders)
                {
                    if (order.CreatedAt.AddHours(4) <= AdminManager.Now)
                    {
                        s_dal.Order.Update(order with { DeliveryDate = AdminManager.Now });
                        var delivery = s_dal.Delivery.ReadAll(x => x.OrderId == order.Id && x.EndTime == null)
                            .FirstOrDefault();
                        if (delivery != null)
                            s_dal.Delivery.Update(delivery with 
                            { 
                                CompletionStatus = DO.DeliveryStatus.Completed, 
                                EndTime = AdminManager.Now 
                            });
                        ordersToNotify.Add(order.Id);
                    }
                }
            }

            // Notify outside of lock
            foreach (var orderId in ordersToNotify)
            {
                Observers.NotifyItemUpdated(orderId);
            }
            if (ordersToNotify.Any())
                Observers.NotifyListUpdated();

            await Task.CompletedTask;
        }
        finally
        {
            s_simulationMutex.UnsetInProgress();
        }
    }

    #region ========== STAGE 7: ASYNC NETWORK OPERATIONS ==========

    /// <summary>
    /// Creates an order with asynchronous geocoding.
    /// Stage 7 - Type A: Single entity network request (Geocoding)
    /// 
    /// Flow:
    /// 1. Geocode the address asynchronously (await)
    /// 2. If Success: Use actual coordinates
    /// 3. If InvalidAddress: Reject order (throw error)
    /// 4. If NetworkError: Use estimated coordinates (continue)
    /// 5. Create order in DB
    /// 6. Send email notifications asynchronously (fire & forget)
    /// </summary>
    public static async Task<(bool success, string? errorMessage, GeocodingService.GeocodingStatus geocodeStatus)> CreateOrderAsync(BO.Order order)
    {
        try
        {
            // Step 1: Validate address is not empty before geocoding
            if (string.IsNullOrWhiteSpace(order.Address))
            {
                return (false, "Address is required. Please enter a valid address.", GeocodingService.GeocodingStatus.InvalidAddress);
            }

            // Step 2: Geocode address asynchronously - await the geocoding service
            var (lat, lon, geocodeStatus) = await GeocodingService.GeocodeAddressAsync(order.Address).ConfigureAwait(false);

            // Step 3: Handle geocoding results
            if (geocodeStatus == GeocodingService.GeocodingStatus.Success)
            {
                // Use actual coordinates
                order.Latitude = lat;
                order.Longitude = lon;
                System.Diagnostics.Debug.WriteLine($"[ORDER] Geocoding successful for: {order.Address}");
            }
            else if (geocodeStatus == GeocodingService.GeocodingStatus.InvalidAddress)
            {
                return (false, $"Address '{order.Address}' could not be found. Please verify the address is correct.", geocodeStatus);
            }
            else if (geocodeStatus == GeocodingService.GeocodingStatus.NetworkError)
            {
                System.Diagnostics.Debug.WriteLine($"[ORDER] Geocoding network error for: {order.Address}. Using estimated location.");
            }

            await Task.CompletedTask.ConfigureAwait(true);

            CreateOrderWithoutNotification(order);
            System.Diagnostics.Debug.WriteLine($"[ORDER] Order created successfully with ID: {order.Id}");

            Observers.NotifyListUpdated();

            _ = EmailHelper.SendNewOrderNotificationToNearbyCouriersAsync(order).ConfigureAwait(false);

            return (true, null, geocodeStatus);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ORDER ERROR] CreateOrderAsync failed: {ex.Message}");
            return (false, $"Error creating order: {ex.Message}", GeocodingService.GeocodingStatus.NetworkError);
        }
    }

    /// <summary>
    /// Helper method - creates order without triggering observers
    /// Used by CreateOrderAsync to avoid lock contention
    /// </summary>
    private static void CreateOrderWithoutNotification(BO.Order order)
    {
        lock (AdminManager.BlMutex)
        {
            DO.Order doOrder = ConvertBOToDO(order);
            s_dal.Order.Create(doOrder);
        }
    }

    /// <summary>
    /// Updates an order with asynchronous geocoding if address changed.
    /// Stage 7 - Type A: Single entity network request (Geocoding)
    /// 
    /// Flow:
    /// 1. Check if address was changed
    /// 2. If changed: Geocode new address asynchronously (await)
    /// 3. Handle geocoding results (same logic as CreateOrderAsync)
    /// 4. Update order in DB
    /// </summary>
    public static async Task<(bool success, string? errorMessage, GeocodingService.GeocodingStatus geocodeStatus)> UpdateOrderAsync(BO.Order order, string? originalAddress)
    {
        try
        {
            GeocodingService.GeocodingStatus geocodeStatus = GeocodingService.GeocodingStatus.NotAttempted;

            // Step 1: Check if address changed
            if (order.Address != originalAddress && !string.IsNullOrWhiteSpace(order.Address))
            {
                System.Diagnostics.Debug.WriteLine($"[ORDER] Address changed from '{originalAddress}' to '{order.Address}'");

                // Step 2: Geocode new address asynchronously - await
                var (lat, lon, status) = await GeocodingService.GeocodeAddressAsync(order.Address).ConfigureAwait(false);
                geocodeStatus = status;

                // Step 3: Handle geocoding results
                if (geocodeStatus == GeocodingService.GeocodingStatus.Success)
                {
                    order.Latitude = lat;
                    order.Longitude = lon;
                    System.Diagnostics.Debug.WriteLine($"[ORDER] New address geocoded successfully");
                }
                else if (geocodeStatus == GeocodingService.GeocodingStatus.InvalidAddress)
                {
                    // Invalid address - DO NOT update the order
                    System.Diagnostics.Debug.WriteLine($"[ORDER] New address is invalid: {order.Address}");
                    return (false, $"New address '{order.Address}' could not be found. Please verify.", geocodeStatus);
                }
                else if (geocodeStatus == GeocodingService.GeocodingStatus.NetworkError)
                {
                    // Network error - update with existing coordinates
                    System.Diagnostics.Debug.WriteLine($"[ORDER] Geocoding network error. Using existing coordinates.");
                }
            }
            else if (order.Address == originalAddress)
            {
                System.Diagnostics.Debug.WriteLine($"[ORDER] Address unchanged, skipping geocoding");
            }

            // ✅ Step 4: CRITICAL - Return to Main Thread before calling Observers
            await Task.CompletedTask.ConfigureAwait(true);

            // ✅ Step 5: Update WITHOUT notification (without Observers!)
            UpdateOrderWithoutNotification(order);
            System.Diagnostics.Debug.WriteLine($"[ORDER] Order updated successfully");

            // ✅ Step 6: Notify observers AFTER update
            Observers.NotifyItemUpdated(order.Id);
            Observers.NotifyListUpdated();

            return (true, null, geocodeStatus);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ORDER ERROR] UpdateOrderAsync failed: {ex.Message}");
            return (false, $"Error updating order: {ex.Message}", GeocodingService.GeocodingStatus.NetworkError);
        }
    }

    /// <summary>
    /// Helper method - updates order without triggering observers
    /// Used by UpdateOrderAsync to avoid lock contention
    /// </summary>
    private static void UpdateOrderWithoutNotification(BO.Order order)
    {
        lock (AdminManager.BlMutex)
        {
            s_dal.Order.Update(ConvertBOToDO(order));
        }
    }

    /// <summary>
    /// Gets available orders for a courier with actual route distances.
    /// Stage 7 - Type B: Collection query with network requests (Distance Calculation)
    /// 
    /// Flow:
    /// 1. Get all available orders
    /// 2. For each order: Calculate route distance asynchronously in parallel (Task.WhenAll)
    /// 3. Filter by max delivery distance
    /// 4. Return sorted by distance
    /// 
    /// Improvements:
    /// - Uses Task.WhenAll for parallel distance calculations
    /// - Uses ConcurrentDictionary cache in GeocodingService to avoid duplicate API calls
    /// </summary>
    public static async Task<IEnumerable<BO.Order>> GetAvailableOrdersWithRouteDistanceAsync(int courierId)
    {
        try
        {
            var courier = CourierManager.ReadCourier(courierId);
            var availableOrders = GetAvailableOrdersForCourier(courierId).ToList();

            if (!availableOrders.Any())
            {
                return availableOrders;
            }

            System.Diagnostics.Debug.WriteLine($"[ORDER] Calculating distances for {availableOrders.Count} orders");

            // Step 1: Calculate distances for all orders in parallel
            // This is Type B: Multiple async network requests in parallel
            var distanceTasks = availableOrders.Select(async order =>
            {
                try
                {
                    // Determine if driving or walking based on delivery type
                    bool isDriving = courier.DeliveryType == BO.DeliveryType.Car || 
                                   courier.DeliveryType == BO.DeliveryType.Motorcycle;

                    // Get route distance asynchronously (uses cache internally)
                    var (distance, isActualRoute) = await GeocodingService.GetRouteDistanceAsync(
                        courier.Location.Latitude, courier.Location.Longitude,
                        order.Latitude, order.Longitude,
                        isDriving
                    ).ConfigureAwait(false);

                    order.ArialDistance = distance;
                    System.Diagnostics.Debug.WriteLine($"[ORDER] Distance for order {order.Id}: {distance:F2} km");
                    return order;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ORDER ERROR] Distance calculation failed for order {order.Id}: {ex.Message}");
                    return order; // Return order with existing distance
                }
            });

            // Step 2: Wait for all distance calculations to complete
            var ordersWithDistances = await Task.WhenAll(distanceTasks).ConfigureAwait(false);

            // Step 3: Filter by max delivery distance and sort
            if (courier.MaxDeliveryDistance.HasValue)
            {
                return ordersWithDistances
                    .Where(o => o.ArialDistance <= courier.MaxDeliveryDistance.Value)
                    .OrderBy(o => o.ArialDistance)
                    .ToList();
            }

            return ordersWithDistances.OrderBy(o => o.ArialDistance).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ORDER ERROR] GetAvailableOrdersWithRouteDistanceAsync failed: {ex.Message}");
            // Fallback to non-async version with air distance
            return GetAvailableOrdersForCourier(courierId);
        }
    }

    /// <summary>
    /// Gets order list with route distances calculated asynchronously.
    /// Stage 7 - Type B: Collection query with network requests (Distance Calculation)
    /// 
    /// Uses same parallel approach as GetAvailableOrdersWithRouteDistanceAsync
    /// </summary>
    public static async Task<IEnumerable<BO.OrderInList>> GetOrderListWithRouteDistancesAsync()
    {
        try
        {
            var orderList = GetOrderList().ToList();
            var config = AdminManager.GetConfig();

            if (!config.CompanyLatitude.HasValue || !config.CompanyLongitude.HasValue)
            {
                System.Diagnostics.Debug.WriteLine("[ORDER] Company location not set, returning air distances");
                return orderList;
            }

            System.Diagnostics.Debug.WriteLine($"[ORDER] Calculating route distances for {orderList.Count} orders");

            // Calculate distances for all orders in parallel
            var distanceTasks = orderList.Select(async orderInList =>
            {
                try
                {
                    // Read full order to get coordinates
                    var fullOrder = ReadOrder(orderInList.OrderId);
                    
                    // Get route distance asynchronously (uses cache internally)
                    var (distance, isActualRoute) = await GeocodingService.GetRouteDistanceAsync(
                        config.CompanyLatitude.Value, config.CompanyLongitude.Value,
                        fullOrder.Latitude, fullOrder.Longitude,
                        isDriving: true // From company to customer, assume driving
                    ).ConfigureAwait(false);

                    // ✅ Create NEW object with updated Distance (not mutate existing)
                    return new BO.OrderInList
                    {
                        OrderId = orderInList.OrderId,
                        DeliveryId = orderInList.DeliveryId,
                        OrderType = orderInList.OrderType,
                        Distance = distance,  
                        OrderStatus = orderInList.OrderStatus,
                        ScheduleStatus = orderInList.ScheduleStatus,
                        OrderCompletionTime = orderInList.OrderCompletionTime,
                        HandlingTime = orderInList.HandlingTime,
                        TotalDeliveries = orderInList.TotalDeliveries,
                        CustomerName = orderInList.CustomerName,
                        CustomerPhone = orderInList.CustomerPhone,
                        Address = orderInList.Address,
                        CourierName = orderInList.CourierName,
                        CreatedAt = orderInList.CreatedAt
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ORDER ERROR] Distance calculation failed for order {orderInList.OrderId}: {ex.Message}");
                    return orderInList; // Return original with existing distance
                }
            });

            // Wait for all calculations
            var ordersWithDistances = await Task.WhenAll(distanceTasks).ConfigureAwait(false);
            return ordersWithDistances;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ORDER ERROR] GetOrderListWithRouteDistancesAsync failed: {ex.Message}");
            // Fallback to non-async version
            return GetOrderList();
        }
    }

    #endregion
}
