using OAuth2.Client;
using RestSharp.Portable;
using System;
using System.Net.Http;

namespace OAuth2.Infrastructure
{
    public static class RequestFactoryExtensions
    {
        public static IRestClient CreateClient(this IRequestFactory factory, Endpoint endpoint)
        {
            var client = factory.CreateClient();
            client.BaseUrl = new Uri(endpoint.BaseUri);
            return client;
        }

        public static IRestRequest CreateRequest(this IRequestFactory factory, Endpoint endpoint)
        {
            return CreateRequest(factory, endpoint, HttpMethod.Get);
        }

        public static IRestRequest CreateRequest(this IRequestFactory factory, Endpoint endpoint, HttpMethod method)
        {
            var request = factory.CreateRequest(endpoint.Resource);
            request.Method = method;
            return request;
        }
    }
}