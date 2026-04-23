using System;
using System.Linq;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Extensions;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Instagram authentication client.
    /// </summary>
    /// <remarks>
    /// <para>This client uses the legacy Instagram API (<c>api.instagram.com</c>) which is no longer
    /// available. The Instagram Basic Display API was shut down on December 4, 2024.</para>
    /// <para>Meta's official announcement states: "After December 4th, 2024, there will no longer
    /// be a set of Instagram APIs for consumer developer apps." Only business/creator APIs
    /// remain (Instagram API with Facebook Login or Instagram Login).</para>
    /// </remarks>
    /// <seealso href="https://developers.facebook.com/blog/post/2024/09/04/update-on-instagram-basic-display-api/">Instagram Basic Display API Shutdown Announcement (Sep 2024)</seealso>
    /// <seealso href="https://developers.facebook.com/docs/instagram-platform">Instagram Platform Documentation (Meta)</seealso>
    public class InstagramClient : OAuth2Client
    {
        private string _accessTokenResponseContent;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstagramClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public InstagramClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://api.instagram.com",
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
                    BaseUri = "https://api.instagram.com",
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
                    BaseUri = "https://api.instagram.com",
                    Resource = "/oauth/access_token"
                };
            }
        }

        /// <inheritdoc />
        protected override void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
            // Instagram returns userinfo on access_token request
            // Source document
            // http://instagram.com/developer/authentication/
            _accessTokenResponseContent = args.Response.Content;
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(_accessTokenResponseContent);
            var user = doc.RootElement.GetProperty("user");
            var names = user.GetProperty("full_name").GetString().Split(' ');
            var avatarUri = user.GetProperty("profile_picture").GetString();
            return new UserInfo
            {
                Id = user.GetProperty("id").GetStringValue(),
                FirstName = names.Any() ? names.First() : user.GetProperty("username").GetString(),
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
            get { return "Instagram"; }
        }
    }
}