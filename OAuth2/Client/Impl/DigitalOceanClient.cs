using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// DigitalOcean authentication client.
    /// </summary>
    public class DigitalOceanClient : OAuth2Client
    {
        private string _accessToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalOceanClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public DigitalOceanClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "DigitalOcean"; }
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
                    BaseUri = "https://cloud.digitalocean.com",
                    Resource = "/v1/oauth/authorize"
                };
            }
        }

        /// <summary>
        /// Called after the access token is obtained. Stores the raw response for later user info parsing.
        /// </summary>
        protected override void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
             _accessToken = args.Response.Content;
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
                    BaseUri = "https://cloud.digitalocean.com",
                    Resource = "/v1/oauth/token"
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
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        protected override Task<UserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ParseUserInfo(_accessToken));
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;
            var info = response.GetProperty("info");
            return new UserInfo
            {
                Id = response.GetProperty("uid").GetStringValue(),
                FirstName = info.GetProperty("name").GetString(),
                LastName = "",
                Email = info.GetStringOrDefault("email")
            };
        }
    }
}
