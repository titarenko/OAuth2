using System.Collections.Specialized;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Contrib;

namespace OAuth2.Client
{
    /// <summary>
    /// Base class for OAuth (version 1) client implementation.
    /// </summary>
    public abstract class OAuthClient : IClient
    {
        private const string OAuthTokenKey = "oauth_token";
        private const string OAuthTokenSecretKey = "oauth_token_secret";

        private readonly IRequestFactory factory;
        private readonly IClientConfiguration configuration;

        private string secret;

        /// <summary>
        /// Friendly name of provider (OAuth service).
        /// </summary>
        public abstract string ProviderName { get; }

        /// <summary>
        /// Defines URI of service which is called for obtaining request token.
        /// </summary>
        protected abstract Endpoint RequestTokenServiceEndpoint { get; }

        /// <summary>
        /// Defines URI of service which should be called to initiate authentication process.
        /// </summary>
        protected abstract Endpoint LoginServiceEndpoint { get; }

        /// <summary>
        /// Defines URI of service which issues access token.
        /// </summary>
        protected abstract Endpoint AccessTokenServiceEndpoint { get; }

        /// <summary>
        /// Defines URI of service which is called to obtain user information.
        /// </summary>
        protected abstract Endpoint UserInfoServiceEndpoint { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthClient" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        protected OAuthClient(IRequestFactory factory, IClientConfiguration configuration)
        {
            this.factory = factory;
            this.configuration = configuration;
        }

        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process.
        /// You should use this URI when rendering login link.
        /// </summary>
        /// <param name="state">Any additional information needed by application.</param>
        /// <returns></returns>
        public string GetLoginLinkUri(string state = null)
        {
            return GetLoginRequestUri(GetRequestToken(), state);
        }

        /// <summary>
        /// Issues request for request token and returns result.
        /// </summary>
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
            return HttpUtility.ParseQueryString(response.Content);
        }

        /// <summary>
        /// Composes login link URI.
        /// </summary>
        /// <param name="response">Content of response for request token request.</param>
        /// <param name="state">Any additional information needed by application.</param>
        private string GetLoginRequestUri(NameValueCollection response, string state = null)
        {
            var client = factory.NewClient();
            client.BaseUrl = LoginServiceEndpoint.BaseUri;

            var request = factory.NewRequest();
            request.Resource = LoginServiceEndpoint.Resource;
            request.AddParameter(OAuthTokenKey, response[OAuthTokenKey]);
            if (!state.IsEmpty())
            {
                request.AddParameter("state", state);
            }
            secret = response[OAuthTokenSecretKey];            

            return client.BuildUri(request).ToString();
        }

        /// <summary>
        /// Obtains user information using third-party authentication service
        /// using data provided via callback request.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).
        /// <example>Request.QueryString</example></param>
        /// <returns></returns>
        public UserInfo GetUserInfo(NameValueCollection parameters)
        {
            var token = parameters[OAuthTokenKey];
            var verifier = parameters["oauth_verifier"];

            return QueryUserInfo(GetAccessToken(token, verifier));
        }

        /// <summary>
        /// Obtains access token by calling corresponding service.
        /// </summary>
        /// <param name="token">Token posted with callback issued by provider.</param>
        /// <param name="verifier">Verifier posted with callback issued by provider.</param>
        /// <returns>Access token and other extra info.</returns>
        private NameValueCollection GetAccessToken(string token, string verifier)
        {
            var client = factory.NewClient();
            client.BaseUrl = AccessTokenServiceEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForAccessToken(
                configuration.ClientId, configuration.ClientSecret, token, secret, verifier);

            var request = factory.NewRequest();
            request.Resource = AccessTokenServiceEndpoint.Resource;
            request.Method = Method.POST;

            var response = client.Execute(request);
            return HttpUtility.ParseQueryString(response.Content);
        }

        /// <summary>
        /// Queries user info using corresponding service and data received by access token request.
        /// </summary>
        /// <param name="parameters">Access token request result.</param>
        private UserInfo QueryUserInfo(NameValueCollection parameters)
        {
            var token = parameters[OAuthTokenKey];
            var secret = parameters[OAuthTokenSecretKey];

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

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> using content of callback issued by service.
        /// </summary>
        protected abstract UserInfo ParseUserInfo(string content);
    }
}