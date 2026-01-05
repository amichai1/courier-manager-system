namespace BO;

/// <summary>
/// Represents an order item in a list view with essential display and sorting fields.
/// </summary>
public class OrderInList
{
    public int OrderId { get; init; }
    public int? DeliveryId { get; init; }
    public OrderType OrderType { get; init; }
    public double Distance { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public ScheduleStatus ScheduleStatus { get; init; }
    public TimeSpan OrderCompletionTime { get; init; }
    public TimeSpan HandlingTime { get; init; }
    public int TotalDeliveries { get; init; }

    // Additional fields for display
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? Address { get; init; }
    public string? CourierName { get; init; }
    public DateTime CreatedAt { get; init; }
}
