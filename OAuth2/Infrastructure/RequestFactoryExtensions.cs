using OAuth2.Client;
using RestSharp;

namespace OAuth2.Infrastructure
{
    public static class RequestFactoryExtensions
    {
        public static RestClient CreateClient(this IRequestFactory factory, Endpoint endpoint)
        {
            return factory.CreateClient(endpoint.BaseUri);
        }

        public static RestRequest CreateRequest(this IRequestFactory factory, Endpoint endpoint)
        {
            return factory.CreateRequest(endpoint.Resource, Method.Get);
        }

        public static RestRequest CreateRequest(this IRequestFactory factory, Endpoint endpoint, Method method)
        {
            return factory.CreateRequest(endpoint.Resource, method);
        }
    }
}