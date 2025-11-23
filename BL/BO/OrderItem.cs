using Helpers;

namespace BO;

/// <summary>
/// Represents a specific product and its quantity within an order.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; } // Added for easy display
    public double Price { get; set; }
    public int Quantity { get; set; }
    public double TotalPrice { get; set; } // Calculated value

    // Override ToString
    public override string ToString() => this.ToStringProperty();
}
