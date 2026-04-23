using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OAuth2.Configuration;
using OAuth2.Extensions;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// LinkedIn authentication client using OpenID Connect (Sign In with LinkedIn v2).
    /// </summary>
    /// <remarks>
    /// <para>Updated from deprecated v1 API (<c>/uas/oauth2/</c>, <c>/v1/people/~</c> XML)
    /// to current v2 OAuth 2.0 with OpenID Connect (<c>/oauth/v2/</c>, <c>/v2/userinfo</c> JSON).
    /// Requires <c>openid profile email</c> scopes.</para>
    /// </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/linkedin/consumer/integrations/self-serve/sign-in-with-linkedin-v2">Sign In with LinkedIn using OpenID Connect</seealso>
    /// <seealso href="https://learn.microsoft.com/en-us/linkedin/shared/authentication/authorization-code-flow">LinkedIn OAuth 2.0 Authorization Code Flow</seealso>
    public class LinkedInClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedInClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public LinkedInClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://www.linkedin.com",
                    Resource = "/oauth/v2/authorization"
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
                    BaseUri = "https://www.linkedin.com",
                    Resource = "/oauth/v2/accessToken"
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
                    BaseUri = "https://api.linkedin.com",
                    Resource = "/v2/userinfo"
                };
            }
        }

        /// <inheritdoc />
        public override Task<string> GetLoginLinkUriAsync(string? state = null, CancellationToken cancellationToken = default)
        {
            return base.GetLoginLinkUriAsync(state ?? Guid.NewGuid().ToString("N"), cancellationToken);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;

            return new UserInfo
            {
                Id = response.GetProperty("sub").GetString(),
                Email = response.GetStringOrDefault("email"),
                FirstName = response.GetStringOrDefault("given_name"),
                LastName = response.GetStringOrDefault("family_name"),
                AvatarUri =
                    {
                        Small  = response.GetStringOrDefault("picture"),
                        Normal = response.GetStringOrDefault("picture"),
                        Large  = response.GetStringOrDefault("picture")
                    }
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth service).
        /// </summary>
        public override string Name
        {
            get { return "LinkedIn"; }
        }
    }
}
