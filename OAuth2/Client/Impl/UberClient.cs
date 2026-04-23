using System;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Extensions;
using OAuth2.Models;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Uber authentication client.
    /// </summary>
    /// <seealso href="https://developer.uber.com/docs/riders/guides/authentication/introduction">Uber OAuth Documentation</seealso>
    public class UberClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UberClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public UberClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// The provider name
        /// </summary>
        public override string Name
        {
            get
            {
                return "Uber";
            }
        }

        /// <summary>
        /// Defines URI of service which issues access code.
        /// </summary>
        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://login.uber.com",
                    Resource = "/oauth/v2/authorize"
                };
            }
        }

        /// <summary>
        /// Defines URI of service which issues access token.
        /// </summary>
        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://login.uber.com",
                    Resource = "/oauth/v2/token"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "Bearer");
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
                    BaseUri = "https://api.uber.com",
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
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;
            var userInfo = new UserInfo();
            if (response.TryGetPropertyIgnoreCase("first_name", out var firstName))
            {
                userInfo.FirstName = firstName.GetString();
            }

            if (response.TryGetPropertyIgnoreCase("last_name", out var lastName))
            {
                userInfo.LastName = lastName.GetString();
            }

            if (response.TryGetPropertyIgnoreCase("email", out var email))
            {
                userInfo.Email = email.GetString();
            }

            if (response.TryGetPropertyIgnoreCase("picture", out var picture))
            {
                var pictureUri = picture.GetString();
                userInfo.AvatarUri.Small = pictureUri;
                userInfo.AvatarUri.Normal = pictureUri;
                userInfo.AvatarUri.Large = pictureUri;
            }

            return userInfo;
         }
    }
}
