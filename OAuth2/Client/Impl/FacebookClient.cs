using System;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Extensions;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Facebook authentication client.
    /// </summary>
    /// <seealso href="https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow">Facebook OAuth Documentation</seealso>
    public class FacebookClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public FacebookClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
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
                    BaseUri = "https://www.facebook.com",
                    Resource = "/v25.0/dialog/oauth"
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
                    BaseUri = "https://graph.facebook.com",
                    Resource = "/v25.0/oauth/access_token"
                };
            }
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
                    BaseUri = "https://graph.facebook.com",
                    Resource = "/v25.0/me"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.AddParameter("fields", "id,first_name,last_name,email,picture");
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;
            const string avatarUriTemplate = "{0}?type={1}";
            var avatarUri = response.GetProperty("picture").GetProperty("data").GetProperty("url").GetString();
            return new UserInfo
            {
                Id = response.GetProperty("id").GetStringValue(),
                FirstName = response.GetProperty("first_name").GetString(),
                LastName = response.GetProperty("last_name").GetString(),
                Email = response.GetStringOrDefault("email"),
                AvatarUri =
                {
                    Small = !String.IsNullOrWhiteSpace(avatarUri) ? String.Format(avatarUriTemplate, avatarUri, "small") : String.Empty,
                    Normal = !String.IsNullOrWhiteSpace(avatarUri) ? String.Format(avatarUriTemplate, avatarUri, "normal") : String.Empty,
                    Large = !String.IsNullOrWhiteSpace(avatarUri) ? String.Format(avatarUriTemplate, avatarUri, "large") : String.Empty
                }
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Facebook"; }
        }
    }
}