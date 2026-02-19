using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace PL.Validation
{
    /// <summary>
    /// Validation Rules for WPF Binding.
    /// </summary>

    /// <summary>
    /// Validates that input is not empty.
    /// </summary>
    public class RequiredValidationRule : ValidationRule
    {
        public string ErrorMessage { get; set; } = "This field is required.";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value as string;

            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, ErrorMessage);
            }

            return ValidationResult.ValidResult;
        }
    }

    /// <summary>
    /// Validates Israeli phone number format.
    /// </summary>
    public class PhoneValidationRule : ValidationRule
    {
        private static readonly Regex PhoneRegex = new Regex(@"^05[0-8]\d{7}$", RegexOptions.Compiled);

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value as string;

            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, "Phone number is required.");
            }

            // Remove dashes and spaces
            string cleaned = input.Replace("-", "").Replace(" ", "").Trim();

            if (!PhoneRegex.IsMatch(cleaned))
            {
                return new ValidationResult(false, "Phone must be 10 digits starting with 05.");
            }

            return ValidationResult.ValidResult;
        }
    }

    /// <summary>
    /// Validates email format.
    /// </summary>
    public class EmailValidationRule : ValidationRule
    {
        private static readonly string[] ValidSuffixes = { "@delivery.com", "@fastship.co.il", "@express.net", "@gmail.com" };

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value as string;

            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, "Email is required.");
            }

            bool hasValidSuffix = false;
            foreach (var suffix in ValidSuffixes)
            {
                if (input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    hasValidSuffix = true;
                    break;
                }
            }

            if (!hasValidSuffix)
            {
                return new ValidationResult(false, $"Email must end with: {string.Join(", ", ValidSuffixes)}");
            }

            return ValidationResult.ValidResult;
        }
    }

    /// <summary>
    /// Validates positive number input.
    /// </summary>
    public class PositiveNumberValidationRule : ValidationRule
    {
        public double Minimum { get; set; } = 0;
        public string FieldName { get; set; } = "Value";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value as string;

            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, $"{FieldName} is required.");
            }

            if (!double.TryParse(input, NumberStyles.Any, cultureInfo, out double number))
            {
                return new ValidationResult(false, $"{FieldName} must be a valid number.");
            }

            if (number < Minimum)
            {
                return new ValidationResult(false, $"{FieldName} must be at least {Minimum}.");
            }

            return ValidationResult.ValidResult;
        }
    }

    /// <summary>
    /// Validates Israeli ID (9 digits with checksum).
    /// </summary>
    public class IsraeliIdValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value as string;

            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, "ID is required.");
            }

            // Pad to 9 digits
            string padded = input.PadLeft(9, '0');

            if (padded.Length != 9 || !padded.All(char.IsDigit))
            {
                return new ValidationResult(false, "ID must be exactly 9 digits.");
            }

            // Luhn-like checksum validation for Israeli ID
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                int digit = padded[i] - '0';
                int multiplier = (i % 2 == 0) ? 1 : 2;
                int result = digit * multiplier;
                sum += (result > 9) ? (result - 9) : result;
            }

            if (sum % 10 != 0)
            {
                return new ValidationResult(false, "Invalid Israeli ID checksum.");
            }

            return ValidationResult.ValidResult;
        }
    }

    /// <summary>
    /// Validates distance range.
    /// </summary>
    public class DistanceRangeValidationRule : ValidationRule
    {
        public double MinDistance { get; set; } = 10;
        public double MaxDistance { get; set; } = 50;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string? input = value as string;

            // Allow empty for nullable fields
            if (string.IsNullOrWhiteSpace(input))
            {
                return ValidationResult.ValidResult;
            }

            if (!double.TryParse(input, NumberStyles.Any, cultureInfo, out double distance))
            {
                return new ValidationResult(false, "Must be a valid number.");
            }

            if (distance < MinDistance || distance > MaxDistance)
            {
                return new ValidationResult(false, $"Distance must be between {MinDistance}-{MaxDistance} km.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
