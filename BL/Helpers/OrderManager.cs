using BO;
using DalApi;
using DO;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BL.Helpers;

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

        // FIXED: Using BO.Order function as requested
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
                try {
                     if (delivery.CourierId > 0) {
                         var c = s_dal.Courier.Read(delivery.CourierId);
                         if(c!=null) { cName = c.Name; dType = c.DeliveryType; }
                     }
                } catch { }

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

    // --- CRUD ---
    public static void CreateOrder(BO.Order order)
    {
        lock (AdminManager.BlMutex) //stage 7
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
            s_dal.Delivery.Create(new DO.Delivery(0, orderId, courierId, (DO.DeliveryType)c.DeliveryType, AdminManager.Now, 0) { CompletionStatus = null, EndTime = null });
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
            s_dal.Order.Update(s_dal.Order.Read(orderId)! with { CourierId = null, CourierAssociatedDate = null, PickupDate = null, DeliveryDate = null });
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
            return new BO.OrderStatusSummary { OpenCount = all.Count(o => o.OrderStatus == BO.OrderStatus.Open) };
        }
    }

    public static IEnumerable<BO.Order> GetCourierOrders(int courierId)
    {
        lock (AdminManager.BlMutex) //stage 7
            return s_dal.Order.ReadAll(o => o.CourierId == courierId).Select(ConvertDOToBO).ToList();
    }

    // --- PERIODIC UPDATES (Stage 7) - תיקון: הוספת sync version ---
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

    #region Async / Stage 7
    public static async Task<(bool success, string? errorMessage, GeocodingService.GeocodingStatus geocodeStatus)> CreateOrderAsync(BO.Order o) {
        var r = await GeocodingService.GeocodeAddressAsync(o.Address); if(r.status==GeocodingService.GeocodingStatus.Success){o.Latitude=r.lat;o.Longitude=r.lon;} CreateOrder(o); return (true,null,r.status);
    }
    public static async Task<(bool success, string? errorMessage, GeocodingService.GeocodingStatus geocodeStatus)> UpdateOrderAsync(BO.Order o, string? prev) {
         var s = GeocodingService.GeocodingStatus.NotAttempted; if(o.Address!=prev){var r=await GeocodingService.GeocodeAddressAsync(o.Address);s=r.status;if(r.status==0){o.Latitude=r.lat;o.Longitude=r.lon;}} UpdateOrder(o); return (true,null,s);
    }
    public static async Task<IEnumerable<BO.Order>> GetAvailableOrdersWithRouteDistanceAsync(int id) => GetAvailableOrdersForCourier(id);
    public static async Task<IEnumerable<BO.OrderInList>> GetOrderListWithRouteDistancesAsync() => GetOrderList();
    #endregion
}
