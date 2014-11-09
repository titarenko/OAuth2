using System.Net;
using OAuth2.Client;
using RestSharp.Portable;
using System.Threading.Tasks;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// REST client extensions
    /// </summary>
    public static class RestClientExtensions
    {
        /// <summary>
        /// Execute a request and test if the status code is OK 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<IRestResponse> ExecuteAndVerify(this IRestClient client, IRestRequest request)
        {
            var response = await client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK ||
                response.IsEmpty())
            {
                throw new UnexpectedResponseException(response);
            }
            return response;
        }
    }
}