using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client
{
    public class GoogleClient : Client
    {
        public GoogleClient(IRestClient client, IRestRequest request, IConfiguration configuration)
            : base(client, request, configuration)
        {
        }

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

        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://www.googleapis.com",
                    Resource = "/userinfo/email"
                };
            }
        }

        protected override UserInfo ParseUserInfo(string content)
        {
            return new UserInfo
            {
                Email = content.Split('&')[0].Split('=')[1],
            };
        }
    }
}