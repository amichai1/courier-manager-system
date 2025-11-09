namespace DO;

/// <summary>
/// Represents a delivery linking an order to a courier.
/// </summary>
/// <remarks>
/// This record connects an order with the courier who chose to handle it.
/// It tracks the entire delivery process from start to completion, including
/// the actual distance traveled and completion status. For "dummy deliveries"
/// (cancelled orders), the CourierId will be 0 and start/end times will be identical.
/// </remarks>
/// <param name="Id">Auto-incrementing unique delivery number</param>
/// <param name="OrderId">ID of the order being delivered</param>
/// <param name="CourierId">National ID of the courier handling the delivery - 0 for dummy deliveries (cancelled orders)</param>
/// <param name="DeliveryType">Type of delivery method at the time of pickup - remains constant even if courier's type changes</param>
/// <param name="StartTime">Date and time when the order was picked up from the company</param>
/// <param name="ActualDistance">Actual distance traveled in kilometers - null until calculation completes, delivery fails if calculation fails</param>
/// <param name="CompletionStatus">How the delivery ended (Completed/Failed/Cancelled) - null while delivery is in progress</param>
/// <param name="EndTime">Date and time when the delivery ended - null while delivery is in progress</param>
public record Delivery
(
    int Id,
    int OrderId,
    int CourierId,
    DeliveryType DeliveryType,
    DateTime StartTime = default,
    Double? ActualDistance = null
)
{
    /// <summary>
    /// Default parameterless constructor for future use in stage 3 of the project.
    /// </summary>
    public DeliveryStatus? CompletionStatus { get; set; } = null;
    public DateTime? EndTime { get; set; } = null;
    public Delivery() : this(0, 0, 0, DeliveryType.OnFoot,default, null) { }
}