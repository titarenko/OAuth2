using System;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Extensions;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Exact Online authentication client.
    /// </summary>
    /// <seealso href="https://developers.exactonline.com/">Exact Online Developer Documentation</seealso>
    public class ExactOnlineClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExactOnlineClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public ExactOnlineClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Exact Online client name
        /// </summary>
        public override string Name
        {
            get
            {
                return "ExactOnline";
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
                    BaseUri = "https://start.exactonline.nl",
                    Resource = "/api/oauth2/auth"
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
                    BaseUri = "https://start.exactonline.nl",
                    Resource = "/api/oauth2/token"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken!, BearerTokenType);
            args.Request.AddHeader("Accept", "application/json");
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
                    BaseUri = "https://start.exactonline.nl",
                    Resource = "/api/v1/current/Me"
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
            if (response.TryGetProperty("d", out var data))
            {
                var user = data.SelectToken("results[0]") ?? data;
                if (user.ValueKind != JsonValueKind.Object || !user.TryGetProperty("UserID", out var id) ||
                    id.ValueKind != JsonValueKind.String || String.IsNullOrWhiteSpace(id.GetString()))
                    throw new UnexpectedResponseException("UserID");

                var picture = user.GetStringOrDefault("PictureUrl");
                return new UserInfo
                {
                    Id = id.GetString(),
                    ProviderName = Name,
                    FirstName = user.GetStringOrDefault("FirstName"),
                    LastName = user.GetStringOrDefault("LastName"),
                    Email = user.GetStringOrDefault("Email"),
                    AvatarUri = { Small = picture, Normal = picture, Large = picture }
                };
            }

            var userInfo = new UserInfo();
            userInfo.AvatarUri.Normal =
                userInfo.AvatarUri.Large =
                userInfo.AvatarUri.Small = response.SelectToken("images[0].url")?.GetStringValue();

            userInfo.FirstName = response.SelectToken("display_name")?.GetStringValue();
            userInfo.Id = response.SelectToken("id")?.GetStringValue();
            userInfo.Email = response.SelectToken("email")?.GetStringValue();
            userInfo.ProviderName = this.Name;
            return userInfo;
        }
    }
}
