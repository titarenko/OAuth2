using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Extensions;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Todoist authentication client.
    /// </summary>
    /// <remarks>
    /// <para>Updated from deprecated Sync API v6 to the current Todoist API v1 (unified).
    /// Uses <c>GET /api/v1/user</c> with Bearer token for user info instead of the
    /// old <c>API/v6/sync</c> endpoint.</para>
    /// </remarks>
    /// <seealso href="https://developer.todoist.com/api/v1/#tag/Authorization/OAuth">Todoist OAuth Documentation</seealso>
    /// <seealso href="https://developer.todoist.com/api/v1/#tag/User/operation/user_info_api_v1_user_get">Todoist User Info API</seealso>
    public class TodoistClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TodoistClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public TodoistClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TodoistClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="requestOptions">Optional transport-level options such as timeout.</param>
        public TodoistClient(IRequestFactory factory, IClientConfiguration configuration, RequestOptions? requestOptions)
            : base(factory, configuration, requestOptions) { }

        /// <summary>
        /// Defines URI of service which issues access code.
        /// </summary>
        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://app.todoist.com",
                    Resource = "/oauth/authorize"
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
                    BaseUri = "https://api.todoist.com",
                    Resource = "/oauth/access_token"
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
                    BaseUri = "https://api.todoist.com",
                    Resource = "/api/v1/user"
                };
            }
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Todoist"; }
        }

        /// <inheritdoc />
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken!, "Bearer");
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var user = doc.RootElement;
            return new UserInfo
            {
                Id = user.GetProperty("id").GetStringValue(),
                Email = user.GetStringOrDefault("email"),
                LastName = user.GetStringOrDefault("full_name"),
                AvatarUri =
                {
                    Small = user.GetStringOrDefault("avatar_small"),
                    Normal = user.GetStringOrDefault("avatar_medium"),
                    Large = user.GetStringOrDefault("avatar_big"),
                }
            };
        }
    }
}
