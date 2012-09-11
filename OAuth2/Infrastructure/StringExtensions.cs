using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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
            var provider = new MD5CryptoServiceProvider();
            var bytes = Encoding.UTF8.GetBytes(input);
            bytes = provider.ComputeHash(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}