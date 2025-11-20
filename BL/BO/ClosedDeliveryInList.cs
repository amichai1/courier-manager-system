using System;

namespace BO;

public class ClosedDeliveryInList
{
    public int DeliveryId { get; set; }
    public int OrderId { get; set; }
    public OrderType OrderType { get; set; }
    public string OrderAddress { get; set; } = string.Empty;
    public DeliveryType DeliveryType { get; set; }
    public double? ActualDistance { get; set; }
    public TimeSpan TotalHandlingTime { get; set; }
    public DeliveryStatus? DeliveryEndType { get; set; }
}


