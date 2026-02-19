namespace BO;

public class OpenOrderInList
{
    public int? CourierId { get; init; }
    public int OrderId { get; init; }
    public OrderType OrderType { get; init; }
    public double Weight { get; init; }
    public double Volume { get; init; }
    public bool IsFragile { get; init; }
    public string Address { get; init; }
    public double AerialDistance { get; init; }
    public double? ActualDistance { get; init; }
    public DateTime? ExpectedTime { get; init; }
    public ScheduleStatus ScheduleStatus { get; init; }
    public TimeSpan HandlingTime { get; init; }
    public DateTime MaxDeliverdTime { get; init; }
}