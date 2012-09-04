using System;
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
        private const string OAuthTokenSecretKey = "oauth_token_secret";

        private readonly IRequestFactory factory;
        private readonly IClientConfiguration configuration;

        private string secret;

        protected abstract Endpoint RequestTokenServiceEndpoint { get; }

        protected abstract Endpoint LoginServiceEndpoint { get; }

        protected abstract Endpoint AccessTokenServiceEndpoint { get; }

        protected abstract Endpoint UserInfoServiceEndpoint { get; }

        protected OAuthClient(IRequestFactory factory, IClientConfiguration configuration)
        {
            this.factory = factory;
            this.configuration = configuration;
        }

        public abstract string ProviderName { get; }


        public string GetLoginLinkUri(string state = null)
        {
            var requestToken = GetRequestToken();

            if (!string.IsNullOrEmpty(state))
                requestToken["state"] = state;

            return GetLoginRequestUri(requestToken);
        }

        public UserInfo GetUserInfo(NameValueCollection parameters)
        {
            var token = parameters[OAuthTokenKey];
            var verifier = parameters["oauth_verifier"];

            var client = factory.NewClient();
            client.BaseUrl = AccessTokenServiceEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForAccessToken(
                configuration.ClientId, configuration.ClientSecret, token, secret, verifier);

            var request = factory.NewRequest();
            request.Resource = AccessTokenServiceEndpoint.Resource;
            request.Method = Method.POST;

            var response = client.Execute(request);

            return QueryUserInfo(response.Content);
        }

        protected abstract UserInfo ParseUserInfo(string content);

        private string GetLoginRequestUri(NameValueCollection response)
        {
            var client = factory.NewClient();
            client.BaseUrl = LoginServiceEndpoint.BaseUri;

            var request = factory.NewRequest();
            request.Resource = LoginServiceEndpoint.Resource;
            request.AddParameter(OAuthTokenKey, response[OAuthTokenKey]);
            secret = response[OAuthTokenSecretKey];            

            return client.BuildUri(request).ToString();
        }

        private NameValueCollection GetRequestToken()
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

            return queryString;
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

            var result = ParseUserInfo(response.Content);
            result.ProviderName = ProviderName;
            return result;
        }
    }
}