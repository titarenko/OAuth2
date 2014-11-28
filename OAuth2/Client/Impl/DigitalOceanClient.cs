using System;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using System.Threading.Tasks;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// OAuth2 client for Digital Ocean
    /// </summary>
    public class DigitalOceanClient : OAuth2Client
    {
        private string _accessToken;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="configuration"></param>
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
        /// Called just after obtaining response with access token from service.
        /// Allows to read extra data returned along with access token.
        /// </summary>
        protected override void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
             _accessToken = args.Response.GetContent();
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
        /// Defines URI of service which allows to obtain information about user 
        /// who is currently logged in.
        /// </summary>
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Obtains user information using provider API.
        /// </summary>
        protected override async Task<UserInfo> GetUserInfo()
        {
            return await Task<UserInfo>.Factory.StartNew(() => ParseUserInfo(_accessToken));
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> using content received from provider.
        /// </summary>
        /// <param name="content">The content which is received from provider.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            return new UserInfo
            {
                Id = response["uid"].Value<string>(),
                FirstName = response["info"]["name"].Value<string>(),
                LastName = "",
                Email = response["info"]["email"].SafeGet(x => x.Value<string>())
            };
        }
    }
}
