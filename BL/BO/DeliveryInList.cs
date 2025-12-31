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

    /// <summary>
    /// Time elapsed from pickup to delivery in HH:mm format.
    /// Calculated as DeliveryDate - PickupDate when both dates are available.
    /// </summary>
    public string ElapsedTime
    {
        get
        {
            if (PickupDate.HasValue && DeliveryDate.HasValue)
            {
                TimeSpan elapsed = DeliveryDate.Value - PickupDate.Value;
                return elapsed.ToString(@"hh\:mm");
            }
            return "—";
        }
    }

    /// <summary>
    /// Calculates the total elapsed time for averaging purposes.
    /// Returns null if complete data is not available.
    /// </summary>
    public TimeSpan? GetElapsedTimeSpan()
    {
        if (PickupDate.HasValue && DeliveryDate.HasValue)
        {
            return DeliveryDate.Value - PickupDate.Value;
        }
        return null;
    }

    public override string ToString()
    {
        return $"Order #{OrderId} - {CustomerName} ({Status})";
    }
}
