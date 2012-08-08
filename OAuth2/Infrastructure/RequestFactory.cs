using RestSharp;

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
        public IRestClient NewClient()
        {
            return new RestClient();
        }

        /// <summary>
        /// Returns new REST request instance.
        /// </summary>
        public IRestRequest NewRequest()
        {
            return new RestRequest();
        }
    }
}