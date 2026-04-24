using OAuth2.Client;
using RestSharp;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Provides convenience extension methods for <see cref="IRequestFactory"/> using <see cref="Endpoint"/> instances.
    /// </summary>
    public static class RequestFactoryExtensions
    {
        /// <summary>
        /// Creates a <see cref="RestClient"/> configured with the base URI of the specified endpoint.
        /// </summary>
        /// <param name="factory">The request factory.</param>
        /// <param name="endpoint">The endpoint whose base URI is used.</param>
        /// <returns>A configured <see cref="RestClient"/> instance.</returns>
        public static RestClient CreateClient(this IRequestFactory factory, Endpoint endpoint)
        {
            return factory.CreateClient(endpoint.BaseUri);
        }

        /// <summary>
        /// Creates a <see cref="RestRequest"/> for the specified endpoint using <see cref="Method.Get"/>.
        /// </summary>
        /// <param name="factory">The request factory.</param>
        /// <param name="endpoint">The endpoint whose resource path is used.</param>
        /// <returns>A <see cref="RestRequest"/> configured for a GET request.</returns>
        public static RestRequest CreateRequest(this IRequestFactory factory, Endpoint endpoint)
        {
            return factory.CreateRequest(endpoint.Resource, Method.Get);
        }

        /// <summary>
        /// Creates a <see cref="RestRequest"/> for the specified endpoint using the given HTTP method.
        /// </summary>
        /// <param name="factory">The request factory.</param>
        /// <param name="endpoint">The endpoint whose resource path is used.</param>
        /// <param name="method">The HTTP method for the request.</param>
        /// <returns>A <see cref="RestRequest"/> configured with the specified method.</returns>
        public static RestRequest CreateRequest(this IRequestFactory factory, Endpoint endpoint, Method method)
        {
            return factory.CreateRequest(endpoint.Resource, method);
        }
    }
}
