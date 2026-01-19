namespace Dal;
using System.Runtime.CompilerServices;
using System;

/// <summary>
/// In-memory configuration for DalList implementation.
/// Stage 7: All ID and Clock properties synchronized for thread-safe access during simulator.
/// </summary>
internal static class Config
{
    // Running ID Configuration for Order
    internal const int startOrderId = 1;
    private static int nextOrderId = startOrderId;
    
    /// <summary>
    /// Gets the next order ID and increments for next time.
    /// Stage 7: Thread-safe access during simulator execution.
    /// </summary>
    internal static int NextOrderId 
    { 
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => nextOrderId++;
    }

    // Running ID Configuration for Delivery
    internal const int startDeliveryId = 1;
    private static int nextDeliveryId = startDeliveryId;
    
    /// <summary>
    /// Gets the next delivery ID and increments for next time.
    /// Stage 7: Thread-safe access during simulator execution.
    /// </summary>
    internal static int NextDeliveryId 
    { 
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => nextDeliveryId++;
    }

    /// <summary>
    /// System clock for the delivery simulation.
    /// Stage 7: Thread-safe access - updated by simulator thread.
    /// </summary>
    private static DateTime _clock = new DateTime(2026, 1, 21, 14, 0, 0);
    
    internal static DateTime Clock
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _clock;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _clock = value;
    }

    // Manager Credentials
    /// <summary>
    /// Manager ID - Stage 7: Thread-safe access
    /// </summary>
    private static int _managerId = 123456789;
    
    internal static int ManagerId
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _managerId;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _managerId = value;
    }

    /// <summary>
    /// Manager Password - Stage 7: Thread-safe access
    /// </summary>
    private static string _managerPassword = "123456789";
    
    internal static string ManagerPassword
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _managerPassword;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _managerPassword = value;
    }

    // Company Address and Coordinates
    /// <summary>
    /// Company address - Stage 7: Thread-safe access
    /// </summary>
    private static string? _companyAddress = null;
    
    internal static string? CompanyAddress
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _companyAddress;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _companyAddress = value;
    }

    /// <summary>
    /// Company latitude - Stage 7: Thread-safe access
    /// </summary>
    private static double? _companyLatitude = null;
    
    internal static double? CompanyLatitude
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _companyLatitude;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _companyLatitude = value;
    }

    /// <summary>
    /// Company longitude - Stage 7: Thread-safe access
    /// </summary>
    private static double? _companyLongitude = null;
    
    internal static double? CompanyLongitude
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _companyLongitude;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _companyLongitude = value;
    }

    // Distance Limitations
    /// <summary>
    /// Max delivery distance - Stage 7: Thread-safe access
    /// </summary>
    private static double? _maxDeliveryDistance = 50.0;
    
    internal static double? MaxDeliveryDistance
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _maxDeliveryDistance;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _maxDeliveryDistance = value;
    }

    // Average Speeds by Delivery Type (km/h)
    /// <summary>
    /// Car speed - Stage 7: Thread-safe access
    /// </summary>
    private static double _carSpeed = 30.0;
    
    internal static double CarSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _carSpeed;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _carSpeed = value;
    }

    /// <summary>
    /// Motorcycle speed - Stage 7: Thread-safe access
    /// </summary>
    private static double _motorcycleSpeed = 35.0;
    
    internal static double MotorcycleSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _motorcycleSpeed;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _motorcycleSpeed = value;
    }

    /// <summary>
    /// Bicycle speed - Stage 7: Thread-safe access
    /// </summary>
    private static double _bicycleSpeed = 15.0;
    
    internal static double BicycleSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _bicycleSpeed;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _bicycleSpeed = value;
    }

    /// <summary>
    /// On-foot speed - Stage 7: Thread-safe access
    /// </summary>
    private static double _onFootSpeed = 4.0;
    
    internal static double OnFootSpeed
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _onFootSpeed;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _onFootSpeed = value;
    }

    // Time Ranges
    /// <summary>
    /// Max delivery time - Stage 7: Thread-safe access
    /// </summary>
    private static TimeSpan _maxDeliveryTime = TimeSpan.FromHours(2);
    
    internal static TimeSpan MaxDeliveryTime
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _maxDeliveryTime;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _maxDeliveryTime = value;
    }

    /// <summary>
    /// Risk range - Stage 7: Thread-safe access
    /// </summary>
    private static TimeSpan _riskRange = TimeSpan.FromMinutes(90);
    
    internal static TimeSpan RiskRange
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _riskRange;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _riskRange = value;
    }

    /// <summary>
    /// Inactivity range - Stage 7: Thread-safe access
    /// </summary>
    private static TimeSpan _inactivityRange = TimeSpan.FromDays(30);
    
    internal static TimeSpan InactivityRange
    {
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        get => _inactivityRange;
        
        [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
        set => _inactivityRange = value;
    }

    /// <summary>
    /// Resets all configuration properties to their initial values.
    /// Stage 7: Thread-safe reset during simulator operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)] //stage 7
    internal static void Reset()
    {
        // Reset running IDs
        nextOrderId = startOrderId;
        nextDeliveryId = startDeliveryId;
        
        // Reset system clock
        _clock = new DateTime(2026, 1, 21, 14, 0, 0);
        
        // Reset manager credentials
        _managerId = 123456789;
        _managerPassword = "123456789";
        
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
    }
}
