namespace BO;
public class Order
{
    internal object? CustomerLocation;

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
    public OrderStatus OrderStatus { get; init; }
    public ScheduleStatus ScheduleStatus { get; init; }
    public TimeSpan? OrderComplitionTime { get; init; }
    public List<DeliveryPerOrderInList> DeliveryHistory { get; init; } = new();
    public DateTime? CourierAssociatedDate { get; internal set; }
    public DateTime? PickupDate { get; internal set; }
    public DateTime? DeliveryDate { get; internal set; }

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
            
            ── TIMELINE ──
            Created At:              {CreatedAt:yyyy-MM-dd HH:mm:ss}
            Expected Delivery Time:  {ExpectedDeliverdTime:yyyy-MM-dd HH:mm:ss}
            Max Delivery Time:       {MaxDeliveredTime:yyyy-MM-dd HH:mm:ss}
            Courier Associated:      {(CourierAssociatedDate.HasValue ? CourierAssociatedDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not assigned]")}
            Pickup Date:             {(PickupDate.HasValue ? PickupDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not picked up]")}
            Delivery Date:           {(DeliveryDate.HasValue ? DeliveryDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "[Not delivered]")}
            Completion Time:         {(OrderComplitionTime.HasValue ? $"{OrderComplitionTime.Value.TotalHours:F2} hours" : "[Not completed]")}
            
            ── DELIVERY HISTORY ──
            Total Deliveries:        {DeliveryHistory.Count}
            {(DeliveryHistory.Count > 0 ? string.Join("\n            ", DeliveryHistory.Select((d, i) => $"  [{i + 1}] {d}")) : "            [No delivery history]")}
            """;
    }
}
