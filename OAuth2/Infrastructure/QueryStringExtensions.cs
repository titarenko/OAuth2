using System.Collections.Generic;
using System.Linq;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Methods for operating on query strings (e.g. "param1=val1&param2=val2").
    /// </summary>
    public static class QueryStringExtensions
    {
        /// <summary>
        /// Creates query string using names and values of instance's properties.
        /// Note: names are converted from camel to snake case.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public static string ToQueryString(this object instance)
        {
            return instance.GetType().GetProperties().Where(x => x.CanRead)
                .Select(x => new
                {
                    Name = x.Name.FromCamelToSnakeCase(),
                    Value = x.GetValue(instance, null)
                })
                .Where(x => x.Value != null)
                .Select(x => "{0}={1}".Fill(x.Name, x.Value))
                .Join("&");
        }

        /// <summary>
        /// Parses query string to dictionary.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="pairSeparator">The pair separator.</param>
        /// <param name="keyValueSeparator">The key value separator.</param>
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