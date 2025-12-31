namespace BO;

/// <summary>
/// Represents a courier with personal and operational details.
/// </summary>
/// <remarks>
/// This class stores information about a courier in the delivery system,
/// including their contact details, operational status, and delivery capabilities.
/// The courier's ID is their national ID number.
/// </remarks>
public class Courier
{
    // logical status must be a property (no fields in BO)
    public CourierStatus Status { get; set; }

    public required Location Location { get; set; }

    public int Id { get; init; }
    public required string Name { get; set; }
    public required string Phone { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public bool IsActive { get; set; }
    public double? MaxDeliveryDistance { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public DateTime StartWorkingDate { get; init; }
    public int DeliveredOnTime { get; set; }
    public int DeliveredLate { get; set; }
    public OrderInProgress? CurrentOrder { get; set; }
    public double TotalWeightInDelivery { get; internal set; }
    public int OrdersInDelivery { get; internal set; }
    
    /// <summary>
    /// Average delivery time calculated as the mean of all completed deliveries.
    /// Format: "HH:mm" (hours:minutes)
    /// Returns "—" if no completed deliveries exist or data is incomplete.
    /// </summary>
    public string AverageDeliveryTime { get; set; } = "—";

    public override string ToString()
    {
        return $"""
            ID:                    {Id}
            Name:                  {Name}
            Phone:                 {Phone}
            Email:                 {Email}
            Password:              {Password}
            Is Active:             {(IsActive ? "Yes" : "No")}
            Delivery Type:         {DeliveryType}
            Max Delivery Distance: {(MaxDeliveryDistance.HasValue ? $"{MaxDeliveryDistance} km" : "Unlimited")}
            Start Working Date:    {StartWorkingDate:yyyy-MM-dd HH:mm:ss}
            Delivered On Time:     {DeliveredOnTime}
            Delivered Late:        {DeliveredLate}
            Average Delivery Time: {AverageDeliveryTime}
            Location:              Lat={Location.Latitude}, Lon={Location.Longitude}
            Current Order:         {(CurrentOrder?.OrderId ?? 0)}
            Total Weight:          {TotalWeightInDelivery} kg
            Orders In Delivery:    {OrdersInDelivery}
            """;
    }
}
