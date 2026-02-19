namespace Dal;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Static configuration class for list-based DAL implementation.
/// </summary>
internal static class Config
{
    // Running ID Configuration for Order
    internal const int startOrderId = 1;
    private static int nextOrderId = startOrderId;
    
    /// <summary>
    /// Gets the next order ID and increments for next time.
    /// </summary>
    internal static int NextOrderId 
    { 
        [MethodImpl(MethodImplOptions.Synchronized)]        get => nextOrderId++;
    }

    // Running ID Configuration for Delivery
    internal const int startDeliveryId = 1;
    private static int nextDeliveryId = startDeliveryId;
    
    /// <summary>
    /// Gets the next delivery ID and increments for next time.
    /// </summary>
    internal static int NextDeliveryId 
    { 
        [MethodImpl(MethodImplOptions.Synchronized)]        get => nextDeliveryId++;
    }

    /// <summary>
    /// System clock for the delivery simulation.
    /// </summary>
    private static DateTime _clock = new DateTime(2026, 1, 21, 14, 0, 0);
    
    internal static DateTime Clock
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _clock;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _clock = value;
    }

    // Manager Credentials
    /// <summary>
    /// Manager ID    /// </summary>
    private static int _managerId = 123456789;
    
    internal static int ManagerId
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _managerId;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _managerId = value;
    }

    /// <summary>
    /// Manager Password    /// </summary>
    private static string _managerPassword = "123456789"; // Default demo credentials
    
    internal static string ManagerPassword
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _managerPassword;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _managerPassword = value;
    }

    // Company Address and Coordinates
    /// <summary>
    /// Company address    /// </summary>
    private static string? _companyAddress = null;
    
    internal static string? CompanyAddress
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _companyAddress;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _companyAddress = value;
    }

    /// <summary>
    /// Company latitude    /// </summary>
    private static double? _companyLatitude = null;
    
    internal static double? CompanyLatitude
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _companyLatitude;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _companyLatitude = value;
    }

    /// <summary>
    /// Company longitude    /// </summary>
    private static double? _companyLongitude = null;
    
    internal static double? CompanyLongitude
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _companyLongitude;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _companyLongitude = value;
    }

    // Distance Limitations
    /// <summary>
    /// Max delivery distance    /// </summary>
    private static double? _maxDeliveryDistance = 50.0;
    
    internal static double? MaxDeliveryDistance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _maxDeliveryDistance;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _maxDeliveryDistance = value;
    }

    // Average Speeds by Delivery Type (km/h)
    /// <summary>
    /// Car speed    /// </summary>
    private static double _carSpeed = 30.0;
    
    internal static double CarSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _carSpeed;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _carSpeed = value;
    }

    /// <summary>
    /// Motorcycle speed    /// </summary>
    private static double _motorcycleSpeed = 35.0;
    
    internal static double MotorcycleSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _motorcycleSpeed;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _motorcycleSpeed = value;
    }

    /// <summary>
    /// Bicycle speed    /// </summary>
    private static double _bicycleSpeed = 15.0;
    
    internal static double BicycleSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _bicycleSpeed;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _bicycleSpeed = value;
    }

    /// <summary>
    /// On-foot speed    /// </summary>
    private static double _onFootSpeed = 4.0;
    
    internal static double OnFootSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _onFootSpeed;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _onFootSpeed = value;
    }

    // Time Ranges
    /// <summary>
    /// Max delivery time    /// </summary>
    private static TimeSpan _maxDeliveryTime = TimeSpan.FromHours(2);
    
    internal static TimeSpan MaxDeliveryTime
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _maxDeliveryTime;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _maxDeliveryTime = value;
    }

    /// <summary>
    /// Risk range    /// </summary>
    private static TimeSpan _riskRange = TimeSpan.FromMinutes(90);
    
    internal static TimeSpan RiskRange
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _riskRange;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _riskRange = value;
    }

    /// <summary>
    /// Inactivity range    /// </summary>
    private static TimeSpan _inactivityRange = TimeSpan.FromDays(30);
    
    internal static TimeSpan InactivityRange
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _inactivityRange;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _inactivityRange = value;
    }

    // ✅ Simulator Interval in minutes per tick
    /// <summary>
    /// Simulator interval - how many real minutes advance per simulator tick (1 second).
    /// Default: 1 minute per tick
    /// </summary>
    private static int _simulatorIntervalMinutes = 1;
    
    internal static int SimulatorIntervalMinutes
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => _simulatorIntervalMinutes;
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => _simulatorIntervalMinutes = value > 0 ? value : 1;  // Ensure always > 0
    }

    /// <summary>
    /// Resets all configuration properties to their initial values.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]    internal static void Reset()
    {
        // Reset running IDs
        nextOrderId = startOrderId;
        nextDeliveryId = startDeliveryId;
        
        // Reset system clock
        _clock = new DateTime(2026, 1, 21, 14, 0, 0);
        
        // Reset manager credentials
        _managerId = 123456789; // Default demo credentials
        _managerPassword = "123456789"; // Default demo credentials
        
        // Reset company address and coordinates
        _companyAddress = null;
        _companyLatitude = null;
        _companyLongitude = null;
        
        // Reset distance limitation
        _maxDeliveryDistance = 50.0;

        // Reset speeds
        _carSpeed = 30.0;
        _motorcycleSpeed = 35.0;
        _bicycleSpeed = 15.0;
        _onFootSpeed = 4.0;
        
        // Reset time ranges
        _maxDeliveryTime = TimeSpan.FromHours(2);
        _riskRange = TimeSpan.FromMinutes(90);       
        _inactivityRange = TimeSpan.FromDays(30);
        
        // ✅ Reset simulator interval
        _simulatorIntervalMinutes = 1;
    }
}
