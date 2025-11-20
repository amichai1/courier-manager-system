namespace BO;

public class OrderInList
{
    public int? DeliveryId { get; init; }
    public int OrderId { get; init; }
    public OrderType OrderType { get; init; }
    public double Distance { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public ScheduleStatus ScheduleStatus { get; init; }
    public TimeSpan OrderComplitionTime { get; init; }
    public TimeSpan HandlingTime { get; init; }
    public int TotalDeliveries { get; init; }
}