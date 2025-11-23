namespace BO;
public class Order
{
    internal object CustomerLocation;

    public int Id { get; init; }
    public OrderType OrderType { get; set; }
    public string? Description { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double ArialDistance { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
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
}
