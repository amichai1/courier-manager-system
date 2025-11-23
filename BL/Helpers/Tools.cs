namespace Helpers;

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;

/// <summary>
/// Internal static class providing general helper methods and extension methods for the Business Layer (BL).
/// </summary>
internal static class Tools
{
    /// <summary>
    /// An extension method that uses Reflection to generate a detailed string representation of an object,
    /// including deep access into collection properties (like List/IEnumerable).
    /// </summary>
    /// <typeparam name="T">The type of the object (usually a BO entity).</typeparam>
    /// <param name="t">The object instance.</param>
    /// <returns>A formatted string listing all public properties and their values.</returns>
    public static string ToStringProperty<T>(this T t)
    {
        // Null check
        if (t == null)
            return "Null Object";

        Type type = t.GetType();
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"--- {type.Name} Details ---");

        // Iterate over all public properties of the object
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object? value = property.GetValue(t);
            sb.Append($"{property.Name}: ");

            // --- Deep Access Logic for Collections (Lists/Arrays/IEnumerable) ---
            if (value is IEnumerable enumerable and not string)
            {
                // If it's a collection, display its contents recursively or list its elements
                sb.AppendLine("[Collection]");

                int index = 0;
                foreach (object? item in enumerable)
                {
                    if (item == null)
                    {
                        sb.AppendLine($"\t[{index}]: NULL");
                    }
                    else if (item.GetType().IsPrimitive || item is string)
                    {
                        sb.AppendLine($"\t[{index}]: {item}");
                    }
                    else
                    {
                        // Use reflection recursively for complex types within the collection
                        sb.AppendLine($"\t--- Item {index} ({item.GetType().Name}) ---");
                        sb.AppendLine(item.ToStringProperty());
                        sb.AppendLine("\t--- End Item ---");
                    }
                    index++;
                }
            }
            else
            {
                // Simple property: display value directly
                sb.AppendLine(value?.ToString() ?? "NULL");
            }
        }
        sb.AppendLine("--------------------------");
        return sb.ToString();
    }
}