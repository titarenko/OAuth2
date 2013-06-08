using System.Net;
using OAuth2.Client;
using RestSharp;

namespace OAuth2.Infrastructure
{
    public static class RestClientExtensions
    {
        public static IRestResponse ExecuteAndVerify(this IRestClient client, IRestRequest request)
        {
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK ||
                response.Content.IsEmpty())
            {
                throw new UnexpectedResponseException(response);
            }
            return response;
        }
    }
}