namespace Dal;

/// <summary>
/// Static configuration class containing system-wide environment variables and settings.
/// </summary>
/// <remarks>
/// This class manages auto-incrementing IDs, system clock, and various operational parameters
/// for the delivery management system. All properties are static as there is only one instance.
/// </remarks>
internal static class Config
{
    // Running ID Configuration for Order
    internal const int startOrderId = 1;
    private static int nextOrderId = startOrderId;
    internal static int NextOrderId { get => nextOrderId++; }

    // Running ID Configuration for Delivery
    internal const int startDeliveryId = 1;
    private static int nextDeliveyId = startDeliveryId;
    internal static int NextDeliveryId { get => nextDeliveyId++; }

    /// <summary>
    /// System clock for the delivery simulation
    /// </summary>
    /// <remarks>
    /// Maintained separately from the computer's real clock.
    /// Can be initialized and advanced by the manager or simulator.
    /// </remarks>
    internal static DateTime Clock { get; set; } = new DateTime (2025, 01, 01, 8,0,0);

    // Manager Credentials     
    /// <summary>
    /// Manager's credentials - initially set during initialization, can be updated later by manager
    /// </summary>
    internal static int ManagerId { get; set; } = 123456789;
    internal static string ManagerPassword { get; set; } = "123456789";

    // Company Address and Coordinates
    /// <summary>
    /// Full valid address of the company headquarters
    /// </summary>
    /// <remarks>
    /// All couriers depart from and return to this address.
    /// Null until a valid address is set. Orders cannot be opened while null.
    /// Format example: "HaNesiim 7, Petah Tikva, Israel"
    /// </remarks>
    internal static string? CompanyAddress { get; set; } = null;

    /// <summary>
    /// Latitude coordinate of the company address
    /// </summary>
    /// <remarks>
    /// Automatically updated by the business layer when company address changes.
    /// Null while address is invalid.
    /// </remarks>
    internal static double? CompanyLatitude { get; set; } = null;

    /// <summary>
    /// Longitude coordinate of the company address
    /// </summary>
    /// <remarks>
    /// Automatically updated by the business layer when company address changes.
    /// Null while address is invalid.
    /// </remarks>
    internal static double? CompanyLongitude { get; set; } = null;

    // Distance Limitations
    /// <summary>
    /// Maximum general delivery distance in kilometers (air distance)
    /// </summary>
    /// <remarks>
    /// Maximum air distance between company address and order address.
    /// Only orders within this range will be accepted.
    /// Null means no distance limitation.
    /// </remarks>
    internal static double? MaxDeliveryDistance { get; set; } = 50.0;

    // Average Speeds by Delivery Type (km/h)
    /// <summary>
    /// Average speed in km/h
    /// </summary>
    /// <remarks>
    /// Used for calculating delivery times and actual distances.
    /// </remarks>
    internal static double CarSpeed { get; set; } = 30.0;
    internal static double MotorcycleSpeed { get; set; } = 35.0;
    internal static double BicycleSpeed { get; set; } = 15.0;
    internal static double OnFootSpeed { get; set; } = 4.0;

    // Time Ranges
    /// <summary>
    /// Maximum delivery time commitment
    /// </summary>
    /// <remarks>
    /// Company's commitment for delivery time to all customers.
    /// Helps calculate if an order is at risk and track on-time deliveries.
    /// Time units (hours/days) determined by company type.
    /// </remarks>
    internal static TimeSpan MaxDeliveryTime { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Risk time range threshold
    /// </summary>
    /// <remarks>
    /// Time range from which an order is considered at risk.
    /// Order is approaching maximum delivery time but hasn't been delivered yet.
    /// Time units (hours/days) determined by company type.
    /// </remarks>
    internal static TimeSpan RiskRange { get; set; } = TimeSpan.FromMinutes(90);

    /// <summary>
    /// Inactivity time range threshold
    /// </summary>
    /// <remarks>
    /// Time range with no courier activity after which courier is automatically set as inactive.
    /// Courier hasn't completed any deliveries during this time range.
    /// Time units (hours/days) determined by company type.
    /// </remarks>
    internal static TimeSpan InactivityRange { get; set; } = TimeSpan.FromDays(30);

    // Reset Method
    /// <summary>
    /// Resets all configuration properties to their initial values
    /// </summary>
    /// <remarks>
    /// This method restores all settings to their default state,
    /// including resetting running ID counters.
    /// </remarks>
    internal static void Reset ()
    {
        // Reset running IDs
        nextOrderId = startOrderId;
        nextDeliveyId = startDeliveryId;
        
        // Reset system clock
        Clock = new DateTime(2026, 1, 21, 14, 0, 0);
        
        // Reset manager credentials
        ManagerId = 123456789;
        ManagerPassword = "123456789";
        
        // Reset company address and coordinates
        CompanyAddress = null;
        CompanyLatitude = null;
        CompanyLongitude = null;
        
        // Reset distance limitation
        MaxDeliveryDistance = 50.0;

        // Reset speeds
        CarSpeed = 30.0;
        MotorcycleSpeed = 35.0;
        BicycleSpeed = 15.0;
        OnFootSpeed = 4.0;
        
        // Reset time ranges
        MaxDeliveryTime = TimeSpan.FromHours(2);
        RiskRange = TimeSpan.FromMinutes(90);       
        InactivityRange = TimeSpan.FromDays(30);
    }
}
