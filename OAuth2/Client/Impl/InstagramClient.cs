using System.Linq;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Instagram authentication client.
    /// </summary>
    public class InstagramClient : OAuth2Client
    {
        private string responseContent;

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
        
        protected override void AfterGetAccessToken(IRestResponse response)
        {
            base.AfterGetAccessToken(response);
            // Instagram returns userinfo on access_token request
            // Source document 
            // http://instagram.com/developer/authentication/
            responseContent = response.Content;
        }
        
        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(responseContent);
            var names = response["user"]["full_name"].Value<string>().Split(' ');
            return new UserInfo
            {
                Id = response["user"]["id"].Value<string>(),
                FirstName = names.Count() > 0 ? names.First() : response["user"]["username"].Value<string>(),
                LastName = names.Count() > 1 ? names.Last() : string.Empty,
                PhotoUri = response["user"]["profile_picture"].Value<string>()
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string ProviderName
        {
            get { return "Instagram"; }
        }
    }
}