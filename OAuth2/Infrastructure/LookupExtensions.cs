using System.Collections.Generic;
using OAuth2.Client;
using System.Linq;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Extension methods for lookups
    /// </summary>
    public static class LookupExtensions
    {
        /// <summary>
        /// Returns the values for a given key (comma separated) or throws an exception if the key wasn't found or all its values were empty.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetOrThrowUnexpectedResponse(this ILookup<string, string> collection, string key)
        {
            var result = collection[key].ToList();
            if (result.All(x => string.IsNullOrEmpty(x)))
                throw new UnexpectedResponseException(key);
            return string.Join(",", result);
        }
    }
}