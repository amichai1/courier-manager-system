namespace BO;

/// <summary>
/// Represents a delivery item in a list view
/// </summary>
public class DeliveryInList
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PickupDate { get; set; }
    public DateTime? DeliveryDate { get; set; }

    public override string ToString()
    {
        return $"Order #{OrderId} - {CustomerName} ({Status})";
    }
}
