using RestSharp;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Intended for REST client/request creation.
    /// </summary>
    public interface IRequestFactory
    {
        /// <summary>
        /// Returns new REST client instance with the specified base URL.
        /// </summary>
        RestClient CreateClient(string baseUrl);
        
        /// <summary>
        /// Returns new REST request instance.
        /// </summary>
        RestRequest CreateRequest(string resource);

        /// <summary>
        /// Returns new REST request instance with the specified HTTP method.
        /// </summary>
        RestRequest CreateRequest(string resource, Method method);
    }
}