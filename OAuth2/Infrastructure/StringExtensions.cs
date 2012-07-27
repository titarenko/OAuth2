using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Set of extension methods for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts string from camel to snake case.
        /// </summary>
        /// <param name="line">The line.</param>
        public static string FromCamelToSnakeCase(this string line)
        {
            var value = Regex.Replace(line, "([A-Z]+)", match => "_" + match.Groups[1].Value.ToLowerInvariant());
            return value.StartsWith("_") ? value.Substring(1) : value;
        }

        /// <summary>
        /// Alias for <code>string.Format</code>.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static string Fill(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        /// <summary>
        /// Alias for <code>string.Join</code>.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="separator">The separator.</param>
        public static string Join<T>(this IEnumerable<T> enumerable, string separator)
        {
            return string.Join(separator, enumerable);
        }

        /// <summary>
        /// Returns true if given line is null, empty or contains only whitespaces.
        /// </summary>
        /// <param name="line">The line.</param>
        public static bool IsEmpty(this string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }
    }
}