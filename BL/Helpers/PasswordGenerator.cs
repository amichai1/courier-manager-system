using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace Helpers;

/// <summary>
/// Utility class for handling password operations.
/// Includes methods for generating secure passwords and validating password strength.
/// This helper ensures consistent password policies across the application.
/// </summary>
public static class PasswordHelper
{
    private static readonly Random s_random = new();

    /// <summary>
    /// Generates a strong password with uppercase, lowercase, digits, and special characters.
    /// Ensures the generated password meets security standards (random length and mixed characters).
    /// </summary>
    /// <returns>A randomly generated strong password (8-12 characters).</returns>
    public static string GenerateStrongPassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";

        var password = new StringBuilder();

        // Ensure at least one character from each required category
        password.Append(uppercase[s_random.Next(uppercase.Length)]);
        password.Append(lowercase[s_random.Next(lowercase.Length)]);
        password.Append(digits[s_random.Next(digits.Length)]);
        password.Append(special[s_random.Next(special.Length)]);

        // Define pool of all allowed characters
        string allChars = uppercase + lowercase + digits + special;

        // Determine random length (between 8 and 12 characters)
        int targetLength = s_random.Next(8, 13);

        // Fill the remaining length with random characters from the pool
        while (password.Length < targetLength)
        {
            password.Append(allChars[s_random.Next(allChars.Length)]);
        }

        // Shuffle the characters to prevent predictable patterns (e.g., ensuring uppercase isn't always first)
        return new string(password.ToString().OrderBy(c => s_random.Next()).ToArray());
    }

    /// <summary>
    /// Validates if a given password meets the security strength requirements.
    /// Requirements: At least 8 chars, 1 uppercase, 1 lowercase, 1 digit, and 1 special char.
    /// </summary>
    /// <param name="password">The password string to validate.</param>
    /// <returns>True if the password is strong; otherwise, false.</returns>
    public static bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        // Regex explanation:
        // ^                 Start of string
        // (?=.*[a-z])       Lookahead: must contain at least one lowercase letter
        // (?=.*[A-Z])       Lookahead: must contain at least one uppercase letter
        // (?=.*\d)          Lookahead: must contain at least one digit
        // (?=.*[!@#$%^&*])  Lookahead: must contain at least one special character
        // .{8,}             Match at least 8 characters of any type
        // $                 End of string
        var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).{8,}$");

        return regex.IsMatch(password);
    }

    /// <summary>
    /// Encrypts a password using SHA256 hashing algorithm.
    /// The password is stored encrypted in the database for security.
    /// </summary>
    /// <param name="password">The plain text password to encrypt.</param>
    /// <returns>The SHA256 hashed password as a hex string.</returns>
    public static string EncryptPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha256.ComputeHash(bytes);

            // Convert to hex string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// Verifies if a plain text password matches an encrypted password.
    /// </summary>
    /// <param name="plainPassword">The plain text password to verify.</param>
    /// <param name="encryptedPassword">The stored encrypted password.</param>
    /// <returns>True if passwords match; otherwise, false.</returns>
    public static bool VerifyPassword(string plainPassword, string encryptedPassword)
    {
        if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(encryptedPassword))
            return false;

        string hashedInput = EncryptPassword(plainPassword);
        return hashedInput.Equals(encryptedPassword, StringComparison.OrdinalIgnoreCase);
    }
}
