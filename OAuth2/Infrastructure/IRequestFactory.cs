using RestSharp;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Intended for REST client/request creation.
    /// </summary>
    public interface IRequestFactory
    {
        /// <summary>
        /// Returns new REST client instance.
        /// </summary>
        IRestClient CreateClient();
        
        /// <summary>
        /// Returns new REST request instance.
        /// </summary>
        IRestRequest CreateRequest();
    }
}