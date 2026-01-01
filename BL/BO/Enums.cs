namespace BO;
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
/// Types of delivery methods available.
/// </summary>
/// <remarks>
/// Defines the transportation method used by couriers.
/// Affects distance and speed calculations for deliveries.
/// </remarks>
public enum DeliveryType
{
    Car,
    Motorcycle,
    Bicycle,
    OnFoot
}

/// <summary>
/// Order status according to the project requirements:
/// Open - Not currently being handled by any courier and has not yet been closed
/// InProgress - Currently being handled by a courier (includes associated and picked up)
/// Delivered - Order closed, delivered successfully
/// OrderRefused - Order closed, customer refused to accept
/// Canceled - Order closed, was cancelled by manager
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order is open and waiting for a courier to pick it up
    /// </summary>
    Open,

    /// <summary>
    /// Order is being handled by a courier (associated or in delivery)
    /// </summary>
    InProgress,

    /// <summary>
    /// Order was delivered successfully
    /// </summary>
    Delivered,

    /// <summary>
    /// Customer refused to accept the order
    /// </summary>
    OrderRefused,

    /// <summary>
    /// Order was cancelled by manager
    /// </summary>
    Canceled
}

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

/// <summary>
/// On-time status
/// 
/// OnTime - The order is open/processing and there is enough time left in the time period that the company has committed to 
/// or the order is closed and the delivery end time is up to the maximum delivery time(and up to it all)
/// 
/// InRisk - The order is open/processing and it has less than the risk time range left until the maximum delivery time
/// 
/// Late - The order is open/processing and its maximum delivery time has passed and has not yet been closed 
/// or the order is closed and the delivery end time is after the maximum delivery time
/// </summary>
public enum ScheduleStatus
{
    OnTime,
    InRisk,
    Late
}
/// <summary>
/// Defines the logical status of a Courier.
/// </summary>
public enum CourierStatus
{
    Available,
    OnRouteForPickup,
    OnRouteForDelivery,
    Inactive
}
/// <summary>
/// Defines the logical type of the vehicle used by the courier.
/// </summary>
public enum VehicleType
{
    Car,
    Motorcycle,
    Bicycle,
    OnFoot
}

/// <summary>
/// defined time units for various configurations and calculations.
/// </summary>
public enum TimeUnit
{
    Minute,
    Hour,
    Day,
    Month,
    Year
}
