using System;
using System.Text.Json;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Extension methods for <see cref="JsonElement"/> to simplify JSON navigation and value extraction.
    /// </summary>
    internal static class JsonElementExtensions
    {
        /// <summary>
        /// Converts a <see cref="JsonElement"/> of any type to its string representation.
        /// </summary>
        public static string GetStringValue(this JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.GetRawText();
                case JsonValueKind.True:
                    return "True";
                case JsonValueKind.False:
                    return "False";
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    return element.GetRawText();
            }
        }

        /// <summary>
        /// Gets a string value from a named property, returning <c>null</c> if the property does not exist or is null.
        /// </summary>
        public static string GetStringOrDefault(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
                return prop.GetStringValue();
            return null;
        }

        /// <summary>
        /// Navigates a dot-separated path with optional array indexing (e.g. <c>"images[0].url"</c>).
        /// Returns <c>null</c> if any segment is missing.
        /// </summary>
        public static JsonElement? SelectToken(this JsonElement element, string path)
        {
            var current = element;
            foreach (var segment in path.Split('.'))
            {
                var part = segment;
                int bracketIndex = part.IndexOf('[');
                if (bracketIndex >= 0)
                {
                    int closingBracketIndex = part.IndexOf(']', bracketIndex + 1);
                    if (closingBracketIndex < 0 || closingBracketIndex != part.Length - 1)
                        return null;

                    var name = part.Substring(0, bracketIndex);
                    var indexStr = part.Substring(bracketIndex + 1, closingBracketIndex - bracketIndex - 1);
                    if (!String.IsNullOrEmpty(name))
                    {
                        if (!current.TryGetProperty(name, out var arrayProp))
                            return null;
                        current = arrayProp;
                    }
                    if (!Int32.TryParse(indexStr, out var index) || index < 0 || current.ValueKind != JsonValueKind.Array || current.GetArrayLength() <= index)
                        return null;
                    current = current[index];
                }
                else
                {
                    if (!current.TryGetProperty(part, out var next))
                        return null;
                    current = next;
                }
            }
            return current;
        }

        /// <summary>
        /// Tries to get a property by name using case-insensitive comparison.
        /// </summary>
        public static bool TryGetPropertyIgnoreCase(this JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (String.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}
