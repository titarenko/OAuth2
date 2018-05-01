using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OAuth2.Client;
using RestSharp;

namespace OAuth2.Infrastructure
{
    public static class RestClientExtensions
    {
        static IRestResponse VerifyResponse(IRestResponse response)
        {
            if (response.Content.IsEmpty() ||
                (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created))
            {
                throw new UnexpectedResponseException(response);
            }

            return response;
        }

        public static async Task<IRestResponse> ExecuteAndVerifyAsync(this IRestClient client, IRestRequest request, CancellationToken cancellationToken = default)
        {
            return VerifyResponse(await client.ExecuteTaskAsync(request, cancellationToken).ConfigureAwait(false));
        }
    }
}