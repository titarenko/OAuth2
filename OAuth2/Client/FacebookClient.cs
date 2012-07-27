using Newtonsoft.Json.Linq;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client
{
    /// <summary>
    /// Facebook authentication client.
    /// </summary>
    public class FacebookClient : Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookClient"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="request">The request.</param>
        /// <param name="configuration">The configuration.</param>
        public FacebookClient(IRestClient client, IRestRequest request, IConfiguration configuration) : base(client, request, configuration)
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
                    BaseUri = "https://www.facebook.com",
                    Resource = "/dialog/oauth"
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
                    BaseUri = "https://graph.facebook.com",
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
                    BaseUri = "https://graph.facebook.com",
                    Resource = "/me"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void OnGetUserInfo(IRestRequest request)
        {
            request.AddParameter("fields", "id,first_name,last_name,email,picture");
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
                FirstName = response["first_name"].Value<string>(),
                LastName = response["last_name"].Value<string>(),
                Email = response["email"].Value<string>(),
                PhotoUri = response["picture"]["data"]["url"].Value<string>()
            };
        }
    }
}