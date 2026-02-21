namespace Dal;
using System;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using static XMLTools;

/// <summary>
/// Static configuration class containing system-wide environment variables and settings.
/// This implementation reads and writes configuration data to the 'data-config.xml' file
/// to ensure persistence between application runs.
/// </summary>
internal static class Config
{
    internal const string s_data_config_xml = "data-config.xml";
    internal const string s_couriers_xml = "couriers.xml";
    internal const string s_orders_xml = "orders.xml";
    internal const string s_deliveries_xml = "deliveries.xml";

    // ✅ Synchronized:
    internal static int NextOrderId
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => GetAndIncreaseConfigIntVal(s_data_config_xml, "NextOrderId");
        
        [MethodImpl(MethodImplOptions.Synchronized)]        private set => SetConfigIntVal(s_data_config_xml, "NextOrderId", value);
    }

    // Reads, increases, and saves the next Delivery ID.
    internal static int NextDeliveryId
    {
        // The getter reads the value, increases it by 1, and saves the new value back to XML.
        [MethodImpl(MethodImplOptions.Synchronized)]        get => GetAndIncreaseConfigIntVal(s_data_config_xml, "NextDeliveryId");
        
        [MethodImpl(MethodImplOptions.Synchronized)]        private set => SetConfigIntVal(s_data_config_xml, "NextDeliveryId", value);
    }

    /// <summary>
    /// System clock for the delivery simulation.
    /// </summary>
    internal static DateTime Clock
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => GetConfigDateVal(s_data_config_xml, "Clock");
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => SetConfigDateVal(s_data_config_xml, "Clock", value);
    }

    // Manager ID for initial login (Int type).
    internal static int ManagerId
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => GetConfigIntVal(s_data_config_xml, "ManagerId");
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => SetConfigIntVal(s_data_config_xml, "ManagerId", value);
    }

    // Manager Password (String type).
    internal static string ManagerPassword
    {
        get => GetConfigStringVal(s_data_config_xml, "ManagerPassword");
        set => SetConfigStringVal(s_data_config_xml, "ManagerPassword", value);
    }


    // Full valid address of the company headquarters.
    internal static string? CompanyAddress
    {
        get => XMLTools.GetConfigStringVal(s_data_config_xml, "CompanyAddress");
        set => XMLTools.SetConfigStringVal(s_data_config_xml, "CompanyAddress", value ?? ""); // Saves null as empty string
    }

    // Latitude coordinate of the company address.
    internal static double? CompanyLatitude
    {
        get => XMLTools.GetConfigDoubleNullableVal(s_data_config_xml, "CompanyLatitude");
        set => XMLTools.SetConfigDoubleNullableVal(s_data_config_xml, "CompanyLatitude", value);
    }

    // Longitude coordinate of the company address.
    internal static double? CompanyLongitude
    {
        get => XMLTools.GetConfigDoubleNullableVal(s_data_config_xml, "CompanyLongitude");
        set => XMLTools.SetConfigDoubleNullableVal(s_data_config_xml, "CompanyLongitude", value);
    }

    // Maximum general delivery distance in kilometers (air distance).
    internal static double? MaxDeliveryDistance
    {
        get => XMLTools.GetConfigDoubleNullableVal(s_data_config_xml, "MaxDeliveryDistance");
        set => XMLTools.SetConfigDoubleNullableVal(s_data_config_xml, "MaxDeliveryDistance", value);
    }


    // Average speed in km/h for Car.
    internal static double CarSpeed
    {
        get => XMLTools.GetConfigDoubleVal(s_data_config_xml, "CarSpeed");
        set => XMLTools.SetConfigDoubleVal(s_data_config_xml, "CarSpeed", value);
    }

    // Average speed in km/h for Motorcycle.
    internal static double MotorcycleSpeed
    {
        get => XMLTools.GetConfigDoubleVal(s_data_config_xml, "MotorcycleSpeed");
        set => XMLTools.SetConfigDoubleVal(s_data_config_xml, "MotorcycleSpeed", value);
    }

    // Average speed in km/h for Bicycle.
    internal static double BicycleSpeed
    {
        get => XMLTools.GetConfigDoubleVal(s_data_config_xml, "BicycleSpeed");
        set => XMLTools.SetConfigDoubleVal(s_data_config_xml, "BicycleSpeed", value);
    }

    // Average speed in km/h for OnFoot.
    internal static double OnFootSpeed
    {
        get => XMLTools.GetConfigDoubleVal(s_data_config_xml, "OnFootSpeed");
        set => XMLTools.SetConfigDoubleVal(s_data_config_xml, "OnFootSpeed", value);
    }


    // Maximum delivery time commitment.
    internal static TimeSpan MaxDeliveryTime
    {
        get => XMLTools.GetConfigTimeSpanVal(s_data_config_xml, "MaxDeliveryTime");
        set => XMLTools.SetConfigTimeSpanVal(s_data_config_xml, "MaxDeliveryTime", value);
    }

    // Risk time range threshold.
    internal static TimeSpan RiskRange
    {
        get => XMLTools.GetConfigTimeSpanVal(s_data_config_xml, "RiskRange");
        set => XMLTools.SetConfigTimeSpanVal(s_data_config_xml, "RiskRange", value);
    }

    // Inactivity time range threshold.
    internal static TimeSpan InactivityRange
    {
        get => XMLTools.GetConfigTimeSpanVal(s_data_config_xml, "InactivityRange");
        set => XMLTools.SetConfigTimeSpanVal(s_data_config_xml, "InactivityRange", value);
    }

    // ✅ Simulator interval in minutes per tick
    internal static int SimulatorIntervalMinutes
    {
        [MethodImpl(MethodImplOptions.Synchronized)]        get => XMLTools.GetConfigIntValWithDefault(s_data_config_xml, "SimulatorIntervalMinutes", 1);
        
        [MethodImpl(MethodImplOptions.Synchronized)]        set => XMLTools.SetConfigIntVal(s_data_config_xml, "SimulatorIntervalMinutes", value);
    }

    
    // Resets all configuration properties to their initial values by setting the values in XML.
    [MethodImpl(MethodImplOptions.Synchronized)]    internal static void Reset()
    {
        // Resetting Auto-IDs to initial values
        NextOrderId = 1;
        NextDeliveryId = 1;

        // Resetting General Config
        Clock = new DateTime(2026, 1, 21, 14, 0, 0); // Start Clock based on original config
        ManagerId = 123456789; // Default demo credentials
        ManagerPassword = "15e2b0d3c33891ebb0f1ef609ec419420c20e320ce94c65fbc8c3312448eb225"; // SHA256 of "123456789"
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
        
        // ✅ Reset simulator interval
        SimulatorIntervalMinutes = 1;
    }
}
