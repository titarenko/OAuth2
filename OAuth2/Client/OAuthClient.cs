using System.Collections.Specialized;
using OAuth2.Configuration;
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
        private readonly IClientConfiguration configuration;

        protected abstract Endpoint RequestTokenServiceEndpoint { get; }

        protected abstract Endpoint LoginServiceEndpoint { get; }

        protected abstract Endpoint AccessTokenServiceEndpoint { get; }

        protected abstract Endpoint UserInfoServiceEndpoint { get; }

        protected OAuthClient(IRequestFactory factory, IConfigurationManager configurationManager)
        {
            this.factory = factory;
            configuration = configurationManager
                .GetConfigSection<OAuth2ConfigurationSection>("oauth2")
                [GetType().Name];
        }

        public string GetLoginLinkUri()
        {
            return GetLoginRequestUri(GetRequestToken());
        }

        public UserInfo GetUserInfo(NameValueCollection parameters)
        {
            var token = parameters[OAuthTokenKey];
            var verifier = parameters["verifier"];

            var client = factory.NewClient();
            client.BaseUrl = AccessTokenServiceEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForAccessToken(
                configuration.ClientId, configuration.ClientSecret, token, verifier);

            var request = factory.NewRequest();
            request.Resource = AccessTokenServiceEndpoint.Resource;
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
            client.BaseUrl = RequestTokenServiceEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForRequestToken(
                configuration.ClientId, configuration.ClientSecret, configuration.RedirectUri);

            var request = factory.NewRequest();
            request.Resource = RequestTokenServiceEndpoint.Resource;
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
            client.BaseUrl = UserInfoServiceEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForProtectedResource(
                configuration.ClientId, configuration.ClientSecret, token, secret);

            var request = factory.NewRequest();
            request.Resource = UserInfoServiceEndpoint.Resource;

            var response = client.Execute(request);

            return ParseUserInfo(response.Content);
        }
    }
}