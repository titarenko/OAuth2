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
            if (response.Content.IsEmpty() ||
                (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created))
            {
                throw new UnexpectedResponseException(response);
            }
            return response;
        }
    }
}