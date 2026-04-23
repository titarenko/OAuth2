using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Extensions;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Microsoft authentication client using Microsoft Identity Platform (v2.0) and Microsoft Graph.
    /// </summary>
    /// <remarks>
    /// This client uses the current Microsoft Identity Platform (login.microsoftonline.com) and
    /// Microsoft Graph API (graph.microsoft.com). It is the modern alternative to
    /// <see cref="WindowsLiveClient"/> for new integrations, but is <b>not</b> a drop-in replacement —
    /// user IDs and supported scopes differ between the two platforms.
    /// </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow">Microsoft Identity Platform OAuth 2.0 Documentation</seealso>
    public class MicrosoftClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public MicrosoftClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://login.microsoftonline.com",
                    Resource = "/common/oauth2/v2.0/authorize"
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
                    BaseUri = "https://login.microsoftonline.com",
                    Resource = "/common/oauth2/v2.0/token"
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
                    BaseUri = "https://graph.microsoft.com",
                    Resource = "/v1.0/me"
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
            var id = response.GetProperty("id").GetStringValue();
            return new UserInfo
            {
                Id = id,
                FirstName = response.GetStringOrDefault("givenName"),
                LastName = response.GetStringOrDefault("surname"),
                Email = response.GetStringOrDefault("mail")
                        ?? response.GetStringOrDefault("userPrincipalName"),
            };
        }

        /// <inheritdoc />
        public override string Name
        {
            get { return "Microsoft"; }
        }
    }
}
