using System;
using RestSharp;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="IRequestFactory"/>.
    /// </summary>
    public class RequestFactory : IRequestFactory
    {
        private readonly RequestOptions? _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestFactory"/> class.
        /// </summary>
        public RequestFactory()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestFactory"/> class with transport options.
        /// </summary>
        /// <param name="options">Transport-level options applied to every client created by this factory.</param>
        public RequestFactory(RequestOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Returns new REST client instance with the specified base URL.
        /// </summary>
        public RestClient CreateClient(string baseUrl)
        {
            if (String.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("Value cannot be null or empty.", nameof(baseUrl));

            if (_options?.Timeout is null)
                return new RestClient(baseUrl);

            return new RestClient(new RestClientOptions(baseUrl)
            {
                Timeout = _options.Timeout.Value
            });
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
