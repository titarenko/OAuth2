using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Windows Live authentication client.
    /// </summary>
    public class WindowsLiveClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsLiveClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public WindowsLiveClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://login.live.com",
                    Resource = "/oauth20_authorize.srf"
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
                    BaseUri = "https://login.live.com",
                    Resource = "/oauth20_token.srf"
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
                    BaseUri = "https://apis.live.net/v5.0",
                    Resource = "/me"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.AddParameter("access_token", AccessToken);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            const string avatarUriTemplate = @"https://cid-{0}.users.storage.live.com/users/0x{0}/myprofile/expressionprofile/profilephoto:Win8Static,{1},UserTileStatic/MeControlXXLUserTile?ck=2&ex=24";
            return new UserInfo
            {
                Id = response["id"].Value<string>(),
                FirstName = response["first_name"].Value<string>(),
                LastName = response["last_name"].Value<string>(),
                Email = response["emails"]["preferred"].SafeGet(x => x.Value<string>()),
                AvatarUri =
                    {
                        Small = string.Format(avatarUriTemplate, response["id"].Value<string>(), "UserTileSmall"),
                        Normal = string.Format(avatarUriTemplate, response["id"].Value<string>(), "UserTileSmall"),
                        Large = string.Format(avatarUriTemplate, response["id"].Value<string>(), "UserTileLarge")
                    }
            };
        }

        public override string Name
        {
            get { return "WindowsLive"; }
        }
    }
}