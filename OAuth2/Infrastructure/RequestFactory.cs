using System;
using RestSharp;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="IRequestFactory"/>.
    /// </summary>
    public class RequestFactory : IRequestFactory
    {
        /// <summary>
        /// Returns new REST client instance with the specified base URL.
        /// </summary>
        public RestClient CreateClient(string baseUrl)
        {
            if (String.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("Value cannot be null or empty.", nameof(baseUrl));

            return new RestClient(baseUrl);
        }

        /// <summary>
        /// Returns new REST request instance.
        /// </summary>
        public RestRequest CreateRequest(string resource)
        {
            if (String.IsNullOrEmpty(resource))
                throw new ArgumentException("Value cannot be null or empty.", nameof(resource));

            return new RestRequest(resource);
        }

        /// <summary>
        /// Returns new REST request instance with the specified HTTP method.
        /// </summary>
        public RestRequest CreateRequest(string resource, Method method)
        {
            if (String.IsNullOrEmpty(resource))
                throw new ArgumentException("Value cannot be null or empty.", nameof(resource));

            return new RestRequest(resource, method);
        }
    }
}
