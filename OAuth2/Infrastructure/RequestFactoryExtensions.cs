using OAuth2.Client;
using RestSharp.Portable;
using System;
using System.Net.Http;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Extension methods for the <see cref="IRequestFactory"/>.
    /// </summary>
    public static class RequestFactoryExtensions
    {
        /// <summary>
        /// Create a new REST client
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static IRestClient CreateClient(this IRequestFactory factory, Endpoint endpoint)
        {
            var client = factory.CreateClient();
            client.BaseUrl = new Uri(endpoint.BaseUri);
            return client;
        }

        /// <summary>
        /// Creates a new REST request
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static IRestRequest CreateRequest(this IRequestFactory factory, Endpoint endpoint)
        {
            return CreateRequest(factory, endpoint, HttpMethod.Get);
        }

        /// <summary>
        /// Creates a new REST request
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="endpoint"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static IRestRequest CreateRequest(this IRequestFactory factory, Endpoint endpoint, HttpMethod method)
        {
            var request = factory.CreateRequest(endpoint.Resource);
            request.Method = method;
            return request;
        }
    }
}