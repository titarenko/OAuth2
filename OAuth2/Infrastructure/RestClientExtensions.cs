using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OAuth2.Client;
using RestSharp;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Provides extension methods for <see cref="RestClient"/> with response verification.
    /// </summary>
    public static class RestClientExtensions
    {
        static RestResponse VerifyResponse(RestResponse response)
        {
            if (response.Content.IsEmpty() ||
                (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created))
            {
                throw new UnexpectedResponseException(response);
            }

            return response;
        }

        /// <summary>
        /// Executes the request and verifies that the response has content and a success status code.
        /// </summary>
        /// <param name="client">The REST client.</param>
        /// <param name="request">The request to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The verified <see cref="RestResponse"/>.</returns>
        /// <exception cref="OAuth2.Client.UnexpectedResponseException">Thrown when the response is empty or has a non-success status code.</exception>
        public static async Task<RestResponse> ExecuteAndVerifyAsync(this RestClient client, RestRequest request, CancellationToken cancellationToken = default)
        {
            return VerifyResponse(await client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false));
        }
    }
}