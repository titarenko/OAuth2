using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Client
{
    public class TwitterClient : OAuthClient
    {
        public TwitterClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        protected override Endpoint RequestTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.twitter.com",
                    Resource = "/oauth/request_token"
                };
            }
        }

        protected override Endpoint LoginServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.twitter.com",
                    Resource = "/oauth/authenticate"
                };
            }
        }

        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.twitter.com",
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
                    BaseUri = "https://api.twitter.com",
                    Resource = "/account/verify_credentials.json"
                };
            }
        }

        public override string ProviderName
        {
            get { return "Twitter"; }
        }

        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);

            var name = response["name"].Value<string>();
            var index = name.IndexOf(' ');

            string firstName;
            string lastName;
            if (index == -1)
            {
                firstName = name;
                lastName = null;
            }
            else
            {
                firstName = name.Substring(0, index);
                lastName = name.Substring(index + 1);
            }

            return new UserInfo
            {
                Id = response["id"].Value<string>(),
                Email = null,
                PhotoUri = response["profile_image_url"].Value<string>(),
                FirstName = firstName,
                LastName = lastName
            };
        }
    }
}