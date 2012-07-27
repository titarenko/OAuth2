using Newtonsoft.Json.Linq;
using RestSharp;

namespace OAuth2
{
    public class FacebookClient : Client
    {
        public FacebookClient(IRestClient client, IRestRequest request, IConfiguration configuration) : base(client, request, configuration)
        {
        }

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