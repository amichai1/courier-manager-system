namespace DO;

/// <summary>
/// Represents an order in the delivery system.
/// </summary>
/// <remarks>
/// This record stores information about a delivery order, including the destination address,
/// customer details, and package specifications. Each order has an auto-incrementing unique ID.
/// </remarks>
/// <param name="Id">Auto-incrementing unique order number</param>
/// <param name="OrderType">Type of order based on company category</param>
/// <param name="Description">Brief textual description of the order content - can be null</param>
/// <param name="Address">Complete valid address of the order destination (e.g., "HaNesiim 7, Petah Tikva, Israel")</param>
/// <param name="Latitude">Latitude coordinate of the address - calculated automatically by the business layer</param>
/// <param name="Longitude">Longitude coordinate of the address - calculated automatically by the business layer</param>
/// <param name="CustomerName">Full name of the customer placing the order</param>
/// <param name="CustomerPhone">Customer's mobile phone number - exactly 10 digits starting with 0</param>
/// <param name="Weight">Weight of the order in kilograms</param>
/// <param name="Volume">Volume of the order in cubic meters</param>
/// <param name="IsFragile">Whether the order contains fragile items requiring special care</param>
/// <param name="CreatedAt">Date and time when the order was created</param>
public record Order
(
    int Id = 0,
    DateTime CreatedAt = default
)
{
    /// <summary>
    /// Default parameterless constructor for future use in stage 3 of the project.
    /// </summary>
    public new OrderType OrderType { get; set; } = OrderType.Retail;
    public new string? Description { get; set; } = null;
    public new string Address { get; set; } = "";
    public new double Latitude { get; set; } = 0;
    public new double Longitude { get; set; } = 0;
    public new string CustomerName { get; set; } = "";
    public new string CustomerPhone { get; set; } = "";
    public new double Weight { get; set; } = 0;
    public new double Volume { get; set; } = 0;
    public new bool IsFragile { get; set; } = false;
    public Order() : this(0, default) { }
}
