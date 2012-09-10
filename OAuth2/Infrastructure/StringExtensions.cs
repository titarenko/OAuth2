using System.Collections.Generic;
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
        /// <param name="line">The line.</param>
        public static string GetMD5Hash(string input)
        {
            var x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            var bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            var s = new StringBuilder();
            foreach (var b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            return s.ToString();
        }
    }
}