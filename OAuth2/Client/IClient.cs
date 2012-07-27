using OAuth2.Models;

namespace OAuth2.Client
{
    /// <summary>
    /// Defines API for doing user authentication using certain third-party service.
    /// </summary>
    /// <remarks>
    /// Standard flow is:
    /// - client instance generates URI for login link
    /// - hosting app renders page with login link using aforementioned URI
    /// - user clicks login link - this leads to redirect to third-party service site
    /// - user does authentication and allows app access his/her basic information
    /// - third-party service redirects user to hosting app
    /// - hosting app reads user information using <see cref="GetUserInfo"/> method on callback
    /// </remarks>
    public interface IClient
    {
        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process. 
        /// You should use this URI when rendering login link.
        /// </summary>
        string GetAccessCodeRequestUri();

        /// <summary>
        /// Returns access token using given code by querying corresponding service.
        /// </summary>
        /// <param name="code">The code which was obtained from third-party authentication service.</param>
        /// <param name="error">The error which was received from third-party authentication service.</param>
        string GetAccessToken(string code, string error);

        /// <summary>
        /// Obtains user information using third-party authentication service.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        UserInfo GetUserInfo(string accessToken);
    }
}