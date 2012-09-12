using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Twitter authentication client.
    /// </summary>
    public class TwitterClient : OAuthClient
    {
        public TwitterClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Defines URI of service which is called for obtaining request token.
        /// </summary>
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

        /// <summary>
        /// Defines URI of service which should be called to initiate authentication process.
        /// </summary>
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

        /// <summary>
        /// Defines URI of service which issues access token.
        /// </summary>
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

        /// <summary>
        /// Defines URI of service which is called to obtain user information.
        /// </summary>
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

        /// <summary>
        /// Friendly name of provider (OAuth service).
        /// </summary>
        public override string ProviderName
        {
            get { return "Twitter"; }
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo" /> using content of callback issued by service.
        /// </summary>
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