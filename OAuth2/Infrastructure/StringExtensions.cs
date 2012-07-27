using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OAuth2.Infrastructure
{
    public static class StringExtensions
    {
        public static string FromCamelToSnakeCase(this string name)
        {
            var value = Regex.Replace(name, "([A-Z]+)", match => "_" + match.Groups[1].Value.ToLowerInvariant());
            return value.StartsWith("_") ? value.Substring(1) : value;
        }

        public static string Fill(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static string Join<T>(this IEnumerable<T> enumerable, string separator)
        {
            return string.Join(separator, enumerable);
        }

        public static bool IsEmpty(this string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }

        public static IDictionary<string, string> ToDictionary(this string line, char pairSeparator = '&', char keyValueSeparator = '=')
        {
            return (from x in line.Split(pairSeparator)
                    let pair = x.Split(keyValueSeparator)
                    select new
                    {
                        Key = pair[0],
                        Value = pair[1]
                    }).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}