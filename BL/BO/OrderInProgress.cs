namespace BO;

public class OrderInProgress
{
    public int DeliveryId { get; init; }
    public int OrderId { get; init; }
    public OrderType OrderType { get; init; }
    public string? Description { get; set; }
    public string Address { get; init; }
    double? ActualDistance { get; init; }
    public string CustomerName { get; init; }
    public string CustomerPhone { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime StartDeliveryTime { get; init; }
    public DateTime ExepectedDeliveryTime { get; init; }
    public DateTime MaxDeliveryTime { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public ScheduleStatus ScheduleStatus { get; init; }
    public TimeSpan OrderComplitionTime { get; init; }
}
