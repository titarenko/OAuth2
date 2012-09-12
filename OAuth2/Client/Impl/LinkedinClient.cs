using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// LinkedIn authentication client.
    /// </summary>
    public class LinkedInClient : OAuthClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedInClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public LinkedInClient(IRequestFactory factory, IClientConfiguration configuration) 
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Gets the request token service endpoint.
        /// </summary>
        protected override Endpoint RequestTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.linkedin.com",
                    Resource = "/uas/oauth/requestToken"
                };
            }
        }

        /// <summary>
        /// Gets the login service endpoint.
        /// </summary>
        protected override Endpoint LoginServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://www.linkedin.com",
                    Resource = "/uas/oauth/authorize"
                };
            }
        }

        /// <summary>
        /// Gets the access token service endpoint.
        /// </summary>
        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.linkedin.com",
                    Resource = "/uas/oauth/accessToken"
                };
            }
        }

        /// <summary>
        /// Gets the user info service endpoint.
        /// </summary>
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "http://api.linkedin.com",
                    Resource = "/v1/people/~:(id,first-name,last-name,picture-url)"
                };
            }
        }

        /// <summary>
        /// Parses the user info.
        /// </summary>
        /// <param name="content">The content.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Friendly name of provider (OAuth service).
        /// </summary>
        public override string ProviderName
        {
            get { return "LinkedIn"; }
        }
    }
}