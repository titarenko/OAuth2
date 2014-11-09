using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Set of extension methods for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
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


        /// <summary>
        /// Returns MD5 Hash of input.
        /// </summary>
        /// <param name="input">The line.</param>
        public static string GetMd5Hash(this string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            bytes = Org.BouncyCastle.Security.DigestUtilities.CalculateDigest("MD5", bytes);
            return string.Join(string.Empty, bytes.Select(x => x.ToString("x2")));
        }

        /// <summary>
        /// Replacement for HttpUtility.ParseQueryString
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static ILookup<string, string> ParseQueryString(this string input)
        {
            var result = new List<KeyValuePair<string, string>>();
            var entries = input.Split('&');
            foreach (var entry in entries)
            {
                var parts = entry.Split('=');
                var key = parts[0];
                var value = string.Join("=", parts.Skip(1));
                result.Add(new KeyValuePair<string, string>(key, value));
            }
            return result.ToLookup(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}