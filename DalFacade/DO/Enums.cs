namespace DO;

/// <summary>
/// Types of delivery methods available.
/// </summary>
/// <remarks>
/// Defines the transportation method used by couriers.
/// Affects distance and speed calculations for deliveries.
/// </remarks>
public enum DeliveryType
{
    /// <summary>
    /// Delivery by car
    /// </summary>
    Car,

    /// <summary>
    /// Delivery by motorcycle
    /// </summary>
    Motorcycle,

    /// <summary>
    /// Delivery by bicycle
    /// </summary>
    Bicycle,

    /// <summary>
    /// Delivery on foot
    /// </summary>
    OnFoot
}

/// <summary>
/// Types of orders available in the system.
/// </summary>
/// <remarks>
/// Different order categories based on the type of items being delivered.
/// </remarks>
public enum OrderType
{
    /// <summary>
    /// Ready-made food from restaurants
    /// </summary>
    RestaurantFood,

    /// <summary>
    /// Grocery and supermarket products
    /// </summary>
    Groceries,

    /// <summary>
    /// Products from various stores (flowers, pharmacy, gifts, etc.)
    /// </summary>
    Retail
}

/// <summary>
/// Possible delivery completion statuses.
/// </summary>
/// <remarks>
/// Indicates how a delivery process ended and determines the order's state afterwards.
/// </remarks>
public enum DeliveryStatus
{
    /// <summary>
    /// Order was delivered successfully and closed
    /// </summary>
    Completed,

    /// <summary>
    /// Customer refused to accept the order - courier returned it to company and order is closed
    /// </summary>
    CustomerRefused,

    /// <summary>
    /// Order was cancelled by manager - delivery closes and becomes a "dummy delivery" if order wasn't in progress
    /// </summary>
    Cancelled,

    /// <summary>
    /// Customer was not found at destination - current delivery closes and order reopens for another courier
    /// </summary>
    CustomerNotFound,

    /// <summary>
    /// Failed to calculate route distance - delivery closes and order remains open
    /// </summary>
    Failed
}