using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace PL.Converters
{
    /// <summary>
    /// Converts between string (TextBox.Text) and double? (Configuration.MaxDeliveryDistance).
    /// Allows free input - validation is done on save.
    /// </summary>
    public class NullableDoubleToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is double d)
            {
                return d.ToString(culture);
            }

            return value.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string s = (value as string)?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture, out double d))
            {
                return (double?)d;
            }

            return null;
        }
    }

    /// <summary>
    /// Converter for Israeli phone numbers - allows free input, validation on save.
    /// Phone must be exactly 10 digits starting with 05.
    /// </summary>
    public class PhoneNumberConverter : IValueConverter
    {
        private static readonly Regex PhoneRegex = new Regex(@"^05[0-8]\d{7}$", RegexOptions.Compiled);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Allow any input - return as-is for validation on save
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Validates phone number format (10 digits starting with 05)
        /// </summary>
        public static bool IsValid(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            // Remove spaces and dashes
            string cleaned = phone.Replace("-", "").Replace(" ", "").Trim();

            // Must be exactly 10 digits starting with 05
            return PhoneRegex.IsMatch(cleaned);
        }

        /// <summary>
        /// Gets validation error message
        /// </summary>
        public static string GetErrorMessage()
        {
            return "Phone number must be exactly 10 digits starting with 05 (e.g., 0501234567).";
        }
    }

    /// <summary>
    /// Converter for max delivery distance - allows free input, validation on save.
    /// Must be >= 10 km.
    /// </summary>
    public class MaxDistanceConverter : IValueConverter
    {
        public const double MinDistance = 10.0;
        public const double DefaultDistance = 50.0;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return d.ToString(culture);
            }

            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Allow any input - return parsed value or null
            string input = value?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (double.TryParse(input, NumberStyles.Any, culture, out double result))
            {
                return result;
            }

            // Return null for invalid input (not NaN)
            return null;
        }

        /// <summary>
        /// Validates distance value
        /// </summary>
        public static bool IsValid(double? value)
        {
            return value.HasValue && value.Value >= MinDistance;
        }

        /// <summary>
        /// Gets validation error message
        /// </summary>
        public static string GetErrorMessage()
        {
            return $"Max delivery distance must be at least {MinDistance} km.";
        }
    }

    /// <summary>
    /// Converter for email - allows free input, validation on save.
    /// Must end with one of the valid email domains.
    /// </summary>
    public class EmailConverter : IValueConverter
    {
        // Valid email suffixes matching the initialization file
        private static readonly string[] ValidEmailSuffixes = { "@delivery.com", "@fastship.co.il", "@express.net", "@gmail.com" };
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Allow any input
            return value?.ToString()?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        public static bool IsValid(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            string trimmed = email.Trim();

            // Check if email ends with any of the valid suffixes
            string? matchedSuffix = ValidEmailSuffixes
                .FirstOrDefault(suffix => trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

            if (matchedSuffix == null)
            {
                return false;
            }

            string usernamePart = trimmed.Substring(0, trimmed.Length - matchedSuffix.Length);
            return !string.IsNullOrEmpty(usernamePart) && UsernameRegex.IsMatch(usernamePart);
        }

        /// <summary>
        /// Gets validation error message
        /// </summary>
        public static string GetErrorMessage()
        {
            return $"Email must end with one of: {string.Join(", ", ValidEmailSuffixes)}";
        }
    }

    /// <summary>
    /// Converter for weight - allows free input, validation on save.
    /// Must be >= 1 kg.
    /// </summary>
    public class WeightConverter : IValueConverter
    {
        public const double MinWeight = 1.0;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return d.ToString("F2", culture);
            }

            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string input = value?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return 0.0;
            }

            if (double.TryParse(input, NumberStyles.Any, culture, out double result))
            {
                return result;
            }

            return 0.0;
        }

        /// <summary>
        /// Validates weight value
        /// </summary>
        public static bool IsValid(double value)
        {
            return value >= MinWeight;
        }

        /// <summary>
        /// Gets validation error message
        /// </summary>
        public static string GetErrorMessage()
        {
            return $"Weight must be at least {MinWeight} kg.";
        }
    }

    /// <summary>
    /// Converter for volume - allows free input, validation on save.
    /// Must be >= 1 m³.
    /// </summary>
    public class VolumeConverter : IValueConverter
    {
        public const double MinVolume = 1.0;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return d.ToString("F2", culture);
            }

            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string input = value?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return 0.0;
            }

            if (double.TryParse(input, NumberStyles.Any, culture, out double result))
            {
                return result;
            }

            return 0.0;
        }

        /// <summary>
        /// Validates volume value
        /// </summary>
        public static bool IsValid(double value)
        {
            return value >= MinVolume;
        }

        /// <summary>
        /// Gets validation error message
        /// </summary>
        public static string GetErrorMessage()
        {
            return $"Volume must be at least {MinVolume} m³.";
        }
    }

    /// <summary>
    /// Converter for Israeli ID (Teudat Zehut) - allows free input, validation on save.
    /// Must be exactly 9 digits.
    /// </summary>
    public class IsraeliIdConverter : IValueConverter
    {
        private static readonly Regex IdRegex = new Regex(@"^\d{9}$", RegexOptions.Compiled);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int id && id > 0)
            {
                return id.ToString();
            }

            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string input = value?.ToString()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                return 0;
            }

            if (int.TryParse(input, out int result))
            {
                return result;
            }

            return 0;
        }

        /// <summary>
        /// Validates Israeli ID (must be exactly 9 digits)
        /// </summary>
        public static bool IsValid(int id)
        {
            return IdRegex.IsMatch(id.ToString().PadLeft(9, '0'));
        }

        /// <summary>
        /// Gets validation error message
        /// </summary>
        public static string GetErrorMessage()
        {
            return "ID must be exactly 9 digits.";
        }
    }

    /// <summary>
    /// Converter for customer name - allows free input, validation on save.
    /// Must contain only letters (Hebrew/English) and spaces.
    /// </summary>
    public class CustomerNameConverter : IValueConverter
    {
        private static readonly Regex NameRegex = new Regex(@"^[a-zA-Z\u0590-\u05FF\s]+$", RegexOptions.Compiled);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Allow any input
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Validates customer name
        /// </summary>
        public static bool IsValid(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return NameRegex.IsMatch(name.Trim());
        }

        /// <summary>
        /// Gets validation error message
        /// </summary>
        public static string GetErrorMessage()
        {
            return "Name must contain only letters and spaces.";
        }
    }
}
