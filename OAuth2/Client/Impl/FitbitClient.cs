using System;
using System.Linq;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Extensions;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Fitbit authentication client.
    /// </summary>
    /// <seealso href="https://dev.fitbit.com/build/reference/web-api/authorization/">Fitbit OAuth Documentation</seealso>
    public class FitbitClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FitbitClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public FitbitClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://www.fitbit.com",
                    Resource = "/oauth2/authorize"
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
                    BaseUri = "https://api.fitbit.com",
                    Resource = "/oauth2/token"
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
                    BaseUri = "https://api.fitbit.com",
                    Resource = "/1/user/-/profile.json"
                };
            }
        }

        /// <inheritdoc />
        protected override void BeforeGetAccessToken(BeforeAfterRequestArgs args)
        {
            args.Request.Authenticator = new HttpBasicAuthenticator(Configuration.ClientId, Configuration.ClientSecret);
            base.BeforeGetAccessToken(args);
        }

        /// <inheritdoc />
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken!, "Bearer");
            base.BeforeGetUserInfo(args);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var user = doc.RootElement.GetProperty("user");
            var names = (user.GetProperty("fullName").GetString() ?? "").Split(' ');
            var avatarUri = user.GetProperty("avatar").GetString();
            return new UserInfo
            {
                Id = user.GetProperty("encodedId").GetStringValue(),
                FirstName = names.Any() ? names.First() : user.GetProperty("displayName").GetString(),
                LastName = names.Count() > 1 ? names.Last() : String.Empty,
                AvatarUri =
                    {
                        Small = null,
                        Normal = avatarUri,
                        Large = null
                    }
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Fitbit"; }
        }
    }
}
