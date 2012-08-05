using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;

namespace OAuth2.Client
{
    public abstract class OAuthClient : IClient
    {
        private const string OAuthTokenKey = "oauth_token";

        private readonly IRequestFactory factory;
        private readonly ServiceClientConfiguration configuration;

        protected Endpoint RequestTokenRequestEndpoint { get; set; }

        protected Endpoint LoginServiceEndpoint { get; set; }

        protected Endpoint AccessTokenRequestEndpoint { get; set; }

        protected Endpoint UserInfoRequestEndpoint { get; set; }

        protected OAuthClient(IRequestFactory factory, IConfigurationManager configurationManager)
        {
            this.factory = factory;
            configuration = configurationManager
                .GetConfigSection<OAuth2ConfigurationSection>("oauth2")
                .Services[GetType().Name];
        }

        public string GetLoginLinkUri()
        {
            return GetLoginRequestUri(GetRequestToken());
        }

        public UserInfo GetUserInfo(string content)
        {
            var parameters = HttpUtility.ParseQueryString(content);
            var token = parameters[OAuthTokenKey];
            var verifier = parameters["verifier"];

            var client = factory.NewClient();
            client.BaseUrl = AccessTokenRequestEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForAccessToken(
                configuration.ClientId, configuration.ClientSecret, token, verifier);

            var request = factory.NewRequest();
            request.Resource = AccessTokenRequestEndpoint.Resource;
            request.Method = Method.POST;

            var response = client.Execute(request);

            return QueryUserInfo(response.Content);
        }

        protected abstract UserInfo ParseUserInfo(string content);

        private string GetLoginRequestUri(string response)
        {
            var client = factory.NewClient();
            client.BaseUrl = LoginServiceEndpoint.BaseUri;

            var request = factory.NewRequest();
            request.Resource = LoginServiceEndpoint.Resource;
            request.AddParameter(OAuthTokenKey, response);

            return client.BuildUri(request).ToString();
        }

        private string GetRequestToken()
        {
            var client = factory.NewClient();
            client.BaseUrl = RequestTokenRequestEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForRequestToken(
                configuration.ClientId, configuration.ClientSecret, configuration.RedirectUri);

            var request = factory.NewRequest();
            request.Resource = RequestTokenRequestEndpoint.Resource;
            request.Method = Method.POST;

            var response = client.Execute(request);
            var queryString = HttpUtility.ParseQueryString(response.Content);

            return queryString[OAuthTokenKey];
        }

        private UserInfo QueryUserInfo(string content)
        {
            var parameters = HttpUtility.ParseQueryString(content);
            var token = parameters[OAuthTokenKey];
            var secret = parameters["oauth_token_secret"];

            var client = factory.NewClient();
            client.BaseUrl = UserInfoRequestEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForProtectedResource(
                configuration.ClientId, configuration.ClientSecret, token, secret);

            var request = factory.NewRequest();
            request.Resource = UserInfoRequestEndpoint.Resource;

            var response = client.Execute(request);

            return ParseUserInfo(response.Content);
        }
    }
}