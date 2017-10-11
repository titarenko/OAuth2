using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Visual Studio Team Services (VSTS) authentication client.
    /// </summary>
    public class VSTSClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VSTSClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public VSTSClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://app.vssps.visualstudio.com/oauth2",
                    Resource = "/authorize"
                };
            }
        }

        protected override void BeforeGetAccessToken(BeforeAfterRequestArgs args)
        {
            string grantTypeToken = args.Parameters["refresh_token"] != null ? "refresh_token" : "urn:ietf:params:oauth:grant-type:jwt-bearer";

            args.Request.AddObject(new
            {
                client_assertion_type = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                client_assertion = args.Configuration.ClientSecret,
                grant_type = grantTypeToken,
                assertion = args.Parameters["refresh_token"] ?? args.Parameters["code"],
                redirect_uri = args.Configuration.RedirectUri
            });
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
                    BaseUri = "https://app.vssps.visualstudio.com/oauth2",
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
                    BaseUri = "https://app.vssps.visualstudio.com",
                    Resource = "/_apis/profile/profiles/me?api-version=1.0"
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
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// For more information see https://www.visualstudio.com/en-us/docs/integrate/api/shared/profiles
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            string avatarUriTemplate = @"https://app.vssps.visualstudio.com/_apis/Profile/Profiles/{0}/Avatar?size={1}&format=png";

            var response = JObject.Parse(content);
            var userinfo =  new UserInfo
            {
                Id = response["id"].Value<string>(),
                FirstName = response["displayName"].Value<string>(),
                AvatarUri =
                    {
                        Small = string.Format(avatarUriTemplate, response["id"].Value<string>(), "small"),
                        Normal = string.Format(avatarUriTemplate, response["id"].Value<string>(), "medium"),
                        Large = string.Format(avatarUriTemplate, response["id"].Value<string>(), "large")
                    },
                Email = response["emailAddress"].Value<string>()
            };
            return userinfo;
        }

        public override string Name
        {
            get { return "VSTS"; }
        }
    }
}