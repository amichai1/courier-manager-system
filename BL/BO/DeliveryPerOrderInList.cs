namespace BO;

public class DeliveryPerOrderInList
{
    public int DeliveryId { get; init; }
    public int? CourierId { get; init; }
    public string? CourierName { get; init; }
    public DeliveryType DeliveryType { get; init; }
    public DateTime StartTimeDelivery { get; init; }
    public DeliveryStatus? EndType { get; init; }
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// The delivery address where the order was delivered
    /// </summary>
    public string? DeliveryAddress { get; init; }
}



