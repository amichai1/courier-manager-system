namespace Dal;
using System;
using System.Xml.Linq;
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

    // Reads, increases, and saves the next Order ID in the data-config file.
    internal static int NextOrderId
    {
        // The getter reads the value, increases it by 1, and saves the new value back to XML.
        get => GetAndIncreaseConfigIntVal(s_data_config_xml, "NextOrderId");
        // The setter is used only for Reset() to set a specific initial value.
        private set => SetConfigIntVal(s_data_config_xml, "NextOrderId", value);
    }

    // Reads, increases, and saves the next Delivery ID.
    internal static int NextDeliveryId
    {
        // The getter reads the value, increases it by 1, and saves the new value back to XML.
        get => GetAndIncreaseConfigIntVal(s_data_config_xml, "NextDeliveryId");
        // The setter is used only for Reset().
        private set => SetConfigIntVal(s_data_config_xml, "NextDeliveryId", value);
    }

    // System clock for the delivery simulation.
    internal static DateTime Clock
    {
        get => GetConfigDateVal(s_data_config_xml, "Clock");
        set => SetConfigDateVal(s_data_config_xml, "Clock", value);
    }

    // Manager ID for initial login (Int type).
    internal static int ManagerId
    {
        get => GetConfigIntVal(s_data_config_xml, "ManagerId");
        set => SetConfigIntVal(s_data_config_xml, "ManagerId", value);
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

    
    // Resets all configuration properties to their initial values by setting the values in XML.
    internal static void Reset()
    {
        // Resetting Auto-IDs to initial values
        NextOrderId = 1;
        NextDeliveryId = 1;

        // Resetting General Config
        Clock = new DateTime(2025, 1, 1, 8, 0, 0); // Start Clock based on original config
        ManagerId = 123456789;
        ManagerPassword = "Admin123!";

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