namespace Dal;
using DalApi;

/// <summary>
/// Implementation of configuration access interface.
/// </summary>
/// <remarks>
/// Provides a wrapper for accessing the static Config class through the IConfig interface.
/// Exposes only configuration properties needed by upper layers (BL, PL).
/// Internal settings like running ID counters remain private to the DAL.
/// </remarks>
public class ConfigImplementation : IConfig
{
    /// <summary>
    /// System clock for the delivery simulation.
    /// </summary>
    public DateTime Clock
    {
        get => Config.Clock;
        set => Config.Clock = value;
    }

    /// <summary>
    /// Manager's national ID number.
    /// </summary>
    public int ManagerId
    {
        get => Config.ManagerId;
        set => Config.ManagerId = value; 
    }

    /// <summary>
    /// Manager's password.
    /// </summary>
    public string ManagerPassword
    {
        get => Config.ManagerPassword;
        set => Config.ManagerPassword = value;
    }

    /// <summary>
    /// Full valid address of the company headquarters.
    /// </summary>
    public string? CompanyAddress
    {
        get => Config.CompanyAddress;
        set => Config.CompanyAddress = value;
    }

    /// <summary>
    /// Latitude coordinate of the company address.
    /// </summary>
    public double? CompanyLatitude
    {
        get => Config.CompanyLatitude;
        set => Config.CompanyLatitude = value;
    }

    /// <summary>
    /// Longitude coordinate of the company address.
    /// </summary>
    public double? CompanyLongitude
    {
        get => Config.CompanyLongitude;
        set => Config.CompanyLongitude = value;
    }

    /// <summary>
    /// Maximum general delivery distance in kilometers (air distance).
    /// </summary>
    public double? MaxDeliveryDistance
    {
        get => Config.MaxDeliveryDistance;
        set => Config.MaxDeliveryDistance = value;
    }

    /// <summary>
    /// Average driving speed by car in km/h.
    /// </summary>
    public double CarSpeed
    {
        get => Config.CarSpeed;
        set => Config.CarSpeed = value;
    }

    /// <summary>
    /// Average driving speed by motorcycle in km/h.
    /// </summary>
    public double MotorcycleSpeed
    {
        get => Config.MotorcycleSpeed;
        set => Config.MotorcycleSpeed = value;
    }

    /// <summary>
    /// Average riding speed by bicycle in km/h.
    /// </summary>
    public double BicycleSpeed
    {
        get => Config.BicycleSpeed;
        set => Config.BicycleSpeed = value;
    }

    /// <summary>
    /// Average walking speed on foot in km/h.
    /// </summary>
    public double OnFootSpeed
    {
        get => Config.OnFootSpeed;
        set => Config.OnFootSpeed = value;
    }

    /// <summary>
    /// Maximum delivery time commitment.
    /// </summary>
    public TimeSpan MaxDeliveryTime
    {
        get => Config.MaxDeliveryTime;
        set => Config.MaxDeliveryTime = value;
    }

    /// <summary>
    /// Risk time range threshold.
    /// </summary>
    public TimeSpan RiskRange
    {
        get => Config.RiskRange;
        set => Config.RiskRange = value;
    }

    /// <summary>
    /// Inactivity time range threshold.
    /// </summary>
    public TimeSpan InactivityRange
    {
        get => Config.InactivityRange;
        set => Config.InactivityRange = value;
    }

    /// <summary>
    /// Resets all configuration properties to their initial values.
    /// </summary>
    public void Reset()
    {
        Config.Reset();
    }
}