using System.Collections.Specialized;
using OAuth2.Client;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Provides extension methods for <see cref="NameValueCollection"/>.
    /// </summary>
    public static class NameValueCollectionExtensions
    {
        /// <summary>
        /// Retrieves the value associated with the specified key, or throws an <see cref="UnexpectedResponseException"/> if the value is empty.
        /// </summary>
        /// <param name="collection">The collection to search.</param>
        /// <param name="key">The key whose value to retrieve.</param>
        /// <returns>The non-empty value associated with the specified key.</returns>
        /// <exception cref="UnexpectedResponseException">Thrown when the value for <paramref name="key"/> is null or empty.</exception>
        public static string GetOrThrowUnexpectedResponse(this NameValueCollection collection, string key)
        {
            var value = collection[key];
            if (value.IsEmpty())
            {
                throw new UnexpectedResponseException(key);
            }
            return value!; // Non-null: IsEmpty check above guarantees value is non-null and non-whitespace
        }
    }
}
