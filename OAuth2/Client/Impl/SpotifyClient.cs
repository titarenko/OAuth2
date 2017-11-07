using System;

using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Spotify client 
    /// https://developer.spotify.com/web-api/authorization-guide/
    /// https://developer.spotify.com/web-api/endpoint-reference/
    /// </summary>
    public class SpotifyClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public SpotifyClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Spotify client name
        /// </summary>
        public override string Name
        {
            get
            {
                return "Spotify";
            }
        }

        /// <summary>
        /// The access code service endpoint
        /// </summary>
        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://accounts.spotify.com",
                    Resource = "/authorize"
                };
            }
        }

        /// <summary>
        /// The acess token service endpoint
        /// </summary>
        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://accounts.spotify.com",
                    Resource = "/api/token"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "Bearer");
        }

        /// <summary>
        /// Defines URI of service which allows to obtain information about user which is currently logged in.
        /// </summary>
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.spotify.com",
                    Resource = "/v1/me"
                };
            }
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            var userInfo = new UserInfo();
            userInfo.AvatarUri.Normal = 
                userInfo.AvatarUri.Large = 
                userInfo.AvatarUri.Small = response.SelectToken("images[0].url")?.ToString();

            userInfo.FirstName = response.SelectToken("display_name")?.ToString();
            userInfo.Id = response.SelectToken("id")?.ToString();
            userInfo.Email = response.SelectToken("email")?.ToString();
            userInfo.ProviderName = this.Name;
            return userInfo;
        }
    }
}
