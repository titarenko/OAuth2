using System;
using System.Collections.Generic;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Extensions;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Google authentication client.
    /// </summary>
    /// <seealso href="https://developers.google.com/identity/protocols/oauth2/web-server">Google OAuth 2.0 Documentation</seealso>
    public class GoogleClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public GoogleClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://accounts.google.com",
                    Resource = "/o/oauth2/v2/auth"
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
                    BaseUri = "https://oauth2.googleapis.com",
                    Resource = "/token"
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
                    BaseUri = "https://www.googleapis.com",
                    Resource = "/oauth2/v3/userinfo"
                };
            }
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Google"; }
        }

        /// <summary>
        /// Uses Google's documented Bearer scheme instead of the authenticator's default OAuth scheme.
        /// </summary>
        /// <param name="args">Request context for the userinfo request.</param>
        /// <seealso href="https://developers.google.com/identity/openid-connect/reference#userinfo">Google userinfo endpoint</seealso>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken!, BearerTokenType);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;
            var avatarUri = response.GetStringOrDefault("picture");
            const string avatarUriTemplate = "{0}?sz={1}";
            return new UserInfo
            {
                Id = response.GetProperty("sub").GetStringValue(),
                Email = response.GetStringOrDefault("email"),
                EmailVerified = response.GetBooleanOrDefault("email_verified"),
                ProviderData = response.TryGetProperty("hd", out var hostedDomain) && hostedDomain.ValueKind == JsonValueKind.String
                    ? new Dictionary<string, string>(StringComparer.Ordinal) { ["hd"] = hostedDomain.GetString()! }
                    : null,
                FirstName = response.TryGetProperty("given_name", out var firstName) ? firstName.GetString() : null,
                LastName = response.TryGetProperty("family_name", out var lastName) ? lastName.GetString() : null,
                AvatarUri =
                    {
                        Small = !String.IsNullOrWhiteSpace(avatarUri) ? String.Format(avatarUriTemplate, avatarUri, AvatarInfo.SmallSize) : String.Empty,
                        Normal = avatarUri,
                        Large = !String.IsNullOrWhiteSpace(avatarUri) ? String.Format(avatarUriTemplate, avatarUri, AvatarInfo.LargeSize): String.Empty
                    }
            };
        }
    }
}
