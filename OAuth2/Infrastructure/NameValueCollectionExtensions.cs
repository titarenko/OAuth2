using System.Collections.Generic;
using OAuth2.Client;
using System.Linq;

namespace OAuth2.Infrastructure
{
    public static class NameValueCollectionExtensions
    {
        public static string GetOrThrowUnexpectedResponse(this ILookup<string, string> collection, string key)
        {
            var result = collection[key].ToList();
            if (result.All(x => string.IsNullOrEmpty(x)))
                throw new UnexpectedResponseException(key);
            return string.Join(",", result);
        }
    }
}