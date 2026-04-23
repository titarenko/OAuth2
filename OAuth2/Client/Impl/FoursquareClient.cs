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
    /// Foursquare authentication client.
    /// </summary>
    /// <remarks>
    /// <para>Warning: Foursquare deprecated the v2 consumer API, including the /v2/users/self endpoint
    /// used by this client. New applications should use the Foursquare Places API (v3) with API keys.</para>
    /// </remarks>
    /// <seealso href="https://docs.foursquare.com/">Foursquare Developer Documentation</seealso>
    [Obsolete("Foursquare v2 consumer OAuth API is deprecated. The replacement Places API v3 uses API keys, not OAuth.")]
    public class FoursquareClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FoursquareClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public FoursquareClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://foursquare.com",
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
                    BaseUri = "https://foursquare.com",
                    Resource = "/oauth2/access_token"
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
                    BaseUri = "https://api.foursquare.com",
                    Resource = "/v2/users/self"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            // Source documents
            // https://developer.foursquare.com/overview/auth.html
            // https://developer.foursquare.com/overview/versioning
            args.Request.AddParameter("v", System.DateTime.Now.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var user = doc.RootElement.GetProperty("response").GetProperty("user");
            var photo = user.GetProperty("photo");
            var prefix = photo.GetProperty("prefix").GetString();
            var suffix = photo.GetProperty("suffix").GetString();
            const string avatarUriTemplate = "{0}{1}{2}";
            const string avatarSizeTemplate = "{0}x{0}";
            return new UserInfo
            {

                Id = user.GetProperty("id").GetStringValue(),
                FirstName = user.GetProperty("firstName").GetString(),
                LastName = user.GetProperty("lastName").GetString(),
                Email = user.GetProperty("contact").GetStringOrDefault("email"),
                AvatarUri =
                {
                    // Defined photo sizes https://developer.foursquare.com/docs/responses/photo
                    Small = !String.IsNullOrWhiteSpace(prefix) ? String.Format(avatarUriTemplate, prefix, String.Format(avatarSizeTemplate, AvatarInfo.SmallSize), suffix) : String.Empty,
                    Normal = !String.IsNullOrWhiteSpace(prefix) ? String.Format(avatarUriTemplate, prefix, String.Empty, suffix) : String.Empty,
                    Large = !String.IsNullOrWhiteSpace(prefix) ? String.Format(avatarUriTemplate, prefix, String.Format(avatarSizeTemplate, AvatarInfo.LargeSize), suffix) : String.Empty
                }
            };
        }

        /// <inheritdoc />
        public override string Name
        {
            get { return "Foursquare"; }
        }
    }
}
