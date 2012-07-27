using Newtonsoft.Json.Linq;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client
{
    /// <summary>
    /// Google authentication client.
    /// </summary>
    public class GoogleClient : Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleClient"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="request">The request.</param>
        /// <param name="configuration">The configuration.</param>
        public GoogleClient(IRestClient client, IRestRequest request, IConfiguration configuration)
            : base(client, request, configuration)
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
                    BaseUri = "https://accounts.google.com",
                    Resource = "/o/oauth2/auth"
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
                    BaseUri = "https://accounts.google.com",
                    Resource = "/o/oauth2/token"
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
                    BaseUri = "https://www.googleapis.com",
                    Resource = "/oauth2/v1/userinfo"
                };
            }
        }
        
        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            return new UserInfo
            {
                Id = response["id"].Value<string>(),
                Email = response["email"].Value<string>(),
                FirstName = response["given_name"].Value<string>(),
                LastName = response["family_name"].Value<string>(),
                PhotoUri = response["picture"].SafeGet(x => x.Value<string>())
            };
        }
    }
}