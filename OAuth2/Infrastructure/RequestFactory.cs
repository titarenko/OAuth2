using RestSharp.Portable;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="IRequestFactory"/>.
    /// </summary>
    public class RequestFactory : IRequestFactory
    {
        /// <summary>
        /// Returns new REST client instance.
        /// </summary>
        public IRestClient CreateClient()
        {
            var client = new RestClient();
            client.IgnoreResponseStatusCode = true;
            return client;
        }

        /// <summary>
        /// Returns new REST request instance.
        /// </summary>
        public IRestRequest CreateRequest(string resource)
        {
            return new RestRequest(resource);
        }
    }
}