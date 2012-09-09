using System.Collections.Specialized;
using OAuth2.Models;

namespace OAuth2.Client
{
    /// <summary>
    /// Defines API for doing user authentication using certain third-party service.
    /// </summary>
    /// <remarks>
    /// Standard flow is:
    /// - client is used for generation of URI for login link (<see cref="GetLoginLinkUri"/>).
    /// - hosting app renders page with login link using aforementioned URI
    /// - user clicks login link - this leads to redirect to third-party service site
    /// - user does authentication and allows app access his/her basic information
    /// - third-party service redirects user to hosting app
    /// - hosting app reads user information using <see cref="GetUserInfo"/> method on callback
    /// </remarks>
    public interface IClient
    {
        /// <summary>
        /// Friendly name of provider (third-party authentication service). 
        /// Defined by client implementation developer and supposed to be unique.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process. 
        /// You should use this URI when rendering login link.
        /// </summary>
        string GetLoginLinkUri(string state = null);

        /// <summary>
        /// Obtains user information using third-party authentication service 
        /// using data provided via callback request.
        /// </summary>
        /// <param name="parameters">
        /// Callback request payload (parameters).
        /// <example>Request.QueryString</example>
        /// </param>
        UserInfo GetUserInfo(NameValueCollection parameters);
    }
}