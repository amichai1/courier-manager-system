namespace BO;
public class Order
{
    // strongly typed BO property (no fields)
    public Location? CustomerLocation { get; set; }

    public int Id { get; init; }
    public OrderType OrderType { get; set; }
    public string? Description { get; set; }
    public required string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double ArialDistance { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public double Weight { get; set; }
    public double Volume { get; set; }
    public bool IsFragile { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpectedDeliverdTime { get; init; }
    public DateTime MaxDeliveredTime { get; init; }

    // Allow BL to derive and update status values
    public OrderStatus OrderStatus { get; set; }
    public ScheduleStatus ScheduleStatus { get; set; }

    public TimeSpan? OrderComplitionTime { get; init; }
    
    // Changed from init to set to allow BL to populate delivery history
    public List<DeliveryPerOrderInList> DeliveryHistory { get; set; } = new();
    
    public DateTime? CourierAssociatedDate { get; internal set; }
    public DateTime? PickupDate { get; internal set; }
    public DateTime? DeliveryDate { get; internal set; }
    public int? CourierId { get; set; }
    public string? CourierName { get; internal set; }

    public override string ToString()
    {
        return $"""
            ID:                      {Id}
            Order Type:              {OrderType}
            Status:                  {OrderStatus}
            Schedule Status:         {ScheduleStatus}
            
            ── CUSTOMER INFORMATION ──
            Customer Name:           {CustomerName}
            Customer Phone:          {CustomerPhone}
            
            ── DELIVERY INFORMATION ──
            Address:                 {Address}
            Location:                Lat={Latitude}, Lon={Longitude}
            Aerial Distance:         {ArialDistance} km
            
            ── PACKAGE DETAILS ──
            Weight:                  {Weight} kg
            Volume:                  {Volume} m³
            Is Fragile:              {(IsFragile ? "Yes" : "No")}
            Description:             {(string.IsNullOrEmpty(Description) ? "[None]" : Description)}
            
            ── COURIER INFORMATION ──
            Courier Name:            {(CourierName != null ? CourierName : "[Not assigned]")}
            
            ── TIMELINE ──
            Created At:              {CreatedAt:yyyy-MM-dd HH:mm:ss}
            Expected Delivery Time:  {ExpectedDeliverdTime:yyyy-MM-dd HH:mm:ss}
            Max Delivery Time:       {MaxDeliveredTime:yyyy-MM-dd HH:mm:ss}
            Courier assigment date:  {(CourierAssociatedDate.HasValue ? CourierAssociatedDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not assigned]")}
            Pickup Date:             {(PickupDate.HasValue ? PickupDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not picked up]")}
            Delivery Date:           {(DeliveryDate.HasValue ? DeliveryDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not delivered]")}
            Completion Time:         {(OrderComplitionTime.HasValue ? $"{OrderComplitionTime.Value.TotalHours:F2} hours" : "[Not completed]")}
            
            ── DELIVERY HISTORY ──
            Total Deliveries:        {DeliveryHistory.Count}
            {(DeliveryHistory.Count > 0 ? string.Join("\n            ", DeliveryHistory.Select((d, i) => $"  [{i + 1}] {d}")) : "            [No delivery history]")}
            """;
    }

    /// <summary>
    /// Calculate air distance between 2 points (Haversine formula)
    /// </summary>
    public static double CalculateAirDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EARTH_RADIUS_KM = 6371;

        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EARTH_RADIUS_KM * c;
    }
}

/// <summary>
/// Summary of order counts by status for dashboard display.
/// </summary>
public class OrderStatusSummary
{
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int DeliveredCount { get; set; }
    public int OrderRefusedCount { get; set; }
    public int CanceledCount { get; set; }

    public int OnTimeCount { get; set; }
    public int InRiskCount { get; set; }
    public int LateCount { get; set; }

    public int TotalCount => OpenCount + InProgressCount + DeliveredCount + OrderRefusedCount + CanceledCount;
}
