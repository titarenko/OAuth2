using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Extensions;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Todoist authentication client.
    /// </summary>
    /// <remarks>
    /// <para>This client uses Todoist's deprecated Sync API v6 (<c>API/v6/sync</c>).
    /// The current API is Todoist API v1 (unified), which replaced Sync API v9 and REST API v2.
    /// The OAuth endpoints are still correct; only the user info endpoint needs updating.</para>
    /// </remarks>
    /// <seealso href="https://developer.todoist.com/guides/#authorization">Todoist OAuth Documentation</seealso>
    /// <seealso href="https://developer.todoist.com/api/v1/">Todoist API v1 Documentation</seealso>
    public class TodoistClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TodoistClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public TodoistClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration) {}

        /// <summary>
        /// Defines URI of service which issues access code.
        /// </summary>
        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://todoist.com/",
                    Resource = "oauth/authorize"
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
                    BaseUri = "https://todoist.com/",
                    Resource = "oauth/access_token"
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
                    BaseUri = "https://todoist.com/",
                    Resource = "API/v6/sync"
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
            args.Request.Authenticator = null;
            args.Request.AddParameter("token", AccessToken, ParameterType.GetOrPost);
            args.Request.AddParameter("resource_types", "[\"all\"]");
            base.BeforeGetUserInfo(args);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var user = doc.RootElement.GetProperty("User");
            return new UserInfo
            {
                Id = user.GetProperty("id").GetStringValue(),
                Email = user.GetStringOrDefault("email"),
                LastName = user.GetProperty("full_name").GetString(),
                AvatarUri =
                {
                    Small = user.GetProperty("avatar_small").GetString(),
                    Normal = user.GetProperty("avatar_medium").GetString(),
                    Large = user.GetProperty("avatar_big").GetString(),
                }
            };
        }
    }
}