namespace Dal;
using DalApi;
using System.Runtime.CompilerServices;

/// <summary>
/// Implementation of configuration access interface.
/// </summary>
/// <remarks>
/// Provides a wrapper for accessing the static Config class through the IConfig interface.
/// Exposes only configuration properties needed by upper layers (BL, PL).
/// Internal settings like running ID counters remain private to the DAL.
/// </remarks>
internal class ConfigImplementation : IConfig
{
    /// <summary>
    /// System clock for the delivery simulation.
    /// </summary>
    public DateTime Clock
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.Clock;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.Clock = value;
    }

    /// <summary>
    /// Manager's national ID number.
    /// </summary>
    public int ManagerId
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.ManagerId;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.ManagerId = value;
    }

    /// <summary>
    /// Manager's password.
    /// </summary>
    public string ManagerPassword
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.ManagerPassword;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.ManagerPassword = value;
    }

    /// <summary>
    /// Full valid address of the company headquarters.
    /// </summary>
    public string? CompanyAddress
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.CompanyAddress;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.CompanyAddress = value;
    }

    /// <summary>
    /// Latitude coordinate of the company address.
    /// </summary>
    public double? CompanyLatitude
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.CompanyLatitude;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.CompanyLatitude = value;
    }

    /// <summary>
    /// Longitude coordinate of the company address.
    /// </summary>
    public double? CompanyLongitude
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.CompanyLongitude;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.CompanyLongitude = value;
    }

    /// <summary>
    /// Maximum general delivery distance in kilometers (air distance).
    /// </summary>
    public double? MaxDeliveryDistance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.MaxDeliveryDistance;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.MaxDeliveryDistance = value;
    }

    /// <summary>
    /// Average driving speed by car in km/h.
    /// </summary>
    public double CarSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.CarSpeed;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.CarSpeed = value;
    }

    /// <summary>
    /// Average driving speed by motorcycle in km/h.
    /// </summary>
    public double MotorcycleSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.MotorcycleSpeed;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.MotorcycleSpeed = value;
    }

    /// <summary>
    /// Average riding speed by bicycle in km/h.
    /// </summary>
    public double BicycleSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.BicycleSpeed;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.BicycleSpeed = value;
    }

    /// <summary>
    /// Average walking speed on foot in km/h.
    /// </summary>
    public double OnFootSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.OnFootSpeed;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.OnFootSpeed = value;
    }

    /// <summary>
    /// Maximum delivery time commitment.
    /// </summary>
    public TimeSpan MaxDeliveryTime
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.MaxDeliveryTime;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.MaxDeliveryTime = value;
    }

    /// <summary>
    /// Risk time range threshold.
    /// </summary>
    public TimeSpan RiskRange
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.RiskRange;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.RiskRange = value;
    }

    /// <summary>
    /// Inactivity time range threshold.
    /// </summary>
    public TimeSpan InactivityRange
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.InactivityRange;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.InactivityRange = value;
    }

    /// <summary>
    /// ✅ Simulator interval in minutes per tick.
    /// </summary>
    public int SimulatorIntervalMinutes
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => Config.SimulatorIntervalMinutes;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set => Config.SimulatorIntervalMinutes = value;
    }

    /// <summary>
    /// Resets all configuration properties to their initial values.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Reset()
    {
        Config.Reset();
    }
}
