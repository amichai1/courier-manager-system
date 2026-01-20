namespace DalApi;

/// <summary>
/// Interface for accessing and managing system configuration settings.
/// </summary>
/// <remarks>
/// Provides access to configuration properties that are used by upper layers (BL, PL).
/// Internal settings like running ID counters are not exposed through this interface.
/// </remarks>
public interface IConfig
{
    /// <summary>
    /// System clock for the delivery simulation.
    /// </summary>
    /// <remarks>
    /// Maintained separately from the computer's real clock.
    /// Can be initialized and advanced by the manager or simulator.
    /// </remarks>
    DateTime Clock { get; set; }

    /// <summary>
    /// Manager's national ID number.
    /// </summary>
    int ManagerId { get; set; }

    /// <summary>
    /// Manager's password.
    /// </summary>
    string ManagerPassword { get; set; }

    /// <summary>
    /// Full valid address of the company headquarters.
    /// </summary>
    /// <remarks>
    /// Null until a valid address is set. Orders cannot be opened while null.
    /// </remarks>
    string? CompanyAddress { get; set; }

    /// <summary>
    /// Latitude coordinate of the company address.
    /// </summary>
    /// <remarks>
    /// Automatically updated by the business layer when company address changes.
    /// Null while address is invalid.
    /// </remarks>
    double? CompanyLatitude { get; set; }

    /// <summary>
    /// Longitude coordinate of the company address.
    /// </summary>
    /// <remarks>
    /// Automatically updated by the business layer when company address changes.
    /// Null while address is invalid.
    /// </remarks>
    double? CompanyLongitude { get; set; }

    /// <summary>
    /// Maximum general delivery distance in kilometers (air distance).
    /// </summary>
    /// <remarks>
    /// Null means no distance limitation.
    /// </remarks>
    double? MaxDeliveryDistance { get; set; }
    
    /// <summary>
    /// Average driving speed by car in km/h.
    /// </summary>
    double CarSpeed { get; set; }

    /// <summary>
    /// Average driving speed by motorcycle in km/h.
    /// </summary>
    double MotorcycleSpeed { get; set; }

    /// <summary>
    /// Average riding speed by bicycle in km/h.
    /// </summary>
    double BicycleSpeed { get; set; }

    /// <summary>
    /// Average walking speed on foot in km/h.
    /// </summary>
    double OnFootSpeed { get; set; }

    /// <summary>
    /// Maximum delivery time commitment.
    /// </summary>
    /// <remarks>
    /// Company's commitment for delivery time to all customers.
    /// </remarks>
    TimeSpan MaxDeliveryTime { get; set; }

    /// <summary>
    /// Risk time range threshold.
    /// </summary>
    /// <remarks>
    /// Time range from which an order is considered at risk.
    /// </remarks>
    TimeSpan RiskRange { get; set; }

    /// <summary>
    /// Inactivity time range threshold.
    /// </summary>
    /// <remarks>
    /// Time range with no courier activity after which courier is automatically set as inactive.
    /// </remarks>
    TimeSpan InactivityRange { get; set; }
    int SimulatorIntervalMinutes { get; set; }

    /// <summary>
    /// Resets all configuration properties to their initial values.
    /// </summary>
    void Reset();
}
