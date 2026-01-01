using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace PL.Converters
{
    /// <summary>
    /// IMultiValueConverter - Combines multiple values into a single display string.
    /// תוספת - שימוש ב-IMultiValueConverter
    /// </summary>
    public class FullAddressMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return string.Empty;

            string address = values[0]?.ToString() ?? "";
            string lat = values[1]?.ToString() ?? "0";
            string lon = values[2]?.ToString() ?? "0";

            if (string.IsNullOrEmpty(address))
                return $"Location: ({lat}, {lon})";

            return $"{address} ({lat}, {lon})";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// IMultiValueConverter - Combines courier info for display.
    /// </summary>
    public class CourierInfoMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "Unknown";

            string name = values[0]?.ToString() ?? "Unknown";
            object deliveryType = values[1];

            return $"{name} ({deliveryType})";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// IMultiValueConverter - Calculates and formats delivery time remaining.
    /// </summary>
    public class DeliveryTimeRemainingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "N/A";

            if (values[0] is DateTime maxTime && values[1] is DateTime currentTime)
            {
                TimeSpan remaining = maxTime - currentTime;

                if (remaining <= TimeSpan.Zero)
                    return "⚠️ OVERDUE";

                if (remaining.TotalHours < 1)
                    return $"⏰ {remaining.Minutes}m remaining";

                return $"⏰ {remaining.Hours}h {remaining.Minutes}m remaining";
            }

            return "N/A";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// MultiTrigger condition converter - returns true when multiple conditions are met.
    /// תוספת - מולטי-טריגר
    /// </summary>
    public class AllTrueMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                return false;

            foreach (var value in values)
            {
                if (value is bool boolValue && !boolValue)
                    return false;
                if (value == DependencyProperty.UnsetValue)
                    return false;
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
