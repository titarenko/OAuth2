using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Extensions;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// X (formerly Twitter) authentication client using OAuth 1.0a.
    /// </summary>
    /// <remarks>
    /// <para>X (formerly Twitter) uses OAuth 1.0a for authentication. The API endpoints at
    /// api.twitter.com are still operational but require at minimum Basic API access tier
    /// ($100/month) for read endpoints. The free tier only supports posting.</para>
    /// <para>Requests user email via <c>include_email=true</c> parameter.
    /// Requires the "Request email address from users" permission enabled in the
    /// X Developer Portal app settings.</para>
    /// </remarks>
    /// <seealso href="https://developer.x.com/en/docs/authentication/oauth-1-0a">X OAuth 1.0a Documentation</seealso>
    public class XClient : OAuthClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public XClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="requestOptions">Optional transport-level options such as timeout.</param>
        public XClient(IRequestFactory factory, IClientConfiguration configuration, RequestOptions? requestOptions)
            : base(factory, configuration, requestOptions)
        {
        }

        /// <summary>
        /// Defines URI of service which is called for obtaining request token.
        /// </summary>
        protected override Endpoint RequestTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.twitter.com",
                    Resource = "/oauth/request_token"
                };
            }
        }

        /// <summary>
        /// Defines URI of service which should be called to initiate authentication process.
        /// </summary>
        protected override Endpoint LoginServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.twitter.com",
                    Resource = "/oauth/authenticate"
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
                    BaseUri = "https://api.twitter.com",
                    Resource = "/oauth/access_token"
                };
            }
        }

        /// <summary>
        /// Defines URI of service which is called to obtain user information.
        /// </summary>
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.twitter.com",
                    Resource = "/1.1/account/verify_credentials.json"
                };
            }
        }

        /// <summary>
        /// Friendly name of provider (OAuth service).
        /// </summary>
        public override string Name
        {
            get { return "X"; }
        }

        /// <summary>
        /// Called just before issuing request to service when everything is ready.
        /// Adds <c>include_email=true</c> to request the user's email address.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.AddQueryParameter("include_email", "true");
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo" /> using content of callback issued by service.
        /// </summary>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;

            var name = response.GetStringOrDefault("name");
            var index = name?.IndexOf(' ') ?? -1;

            string? firstName;
            string? lastName;
            if (index == -1)
            {
                firstName = name;
                lastName = null;
            }
            else
            {
                firstName = name!.Substring(0, index); // Non-null: index != -1 means IndexOf succeeded, so name was non-null
                lastName = name!.Substring(index + 1);
            }
            var avatarUri = response.GetStringOrDefault("profile_image_url");
            return new UserInfo
            {
                Id = response.GetProperty("id").GetStringValue(),
                Email = response.GetStringOrDefault("email"),
                FirstName = firstName,
                LastName = lastName,
                AvatarUri =
                    {
                        Small = avatarUri?.Replace("normal", "mini"),
                        Normal = avatarUri,
                        Large = avatarUri?.Replace("normal", "bigger")
                    }
            };
        }
    }
}
