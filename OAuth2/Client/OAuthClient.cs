using System.Collections.Specialized;
using System.Net;
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

        private readonly IRequestFactory _factory;
        private readonly IClientConfiguration _configuration;

        private string _secret;

        /// <summary>
        /// Friendly name of provider (OAuth service).
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// State which was posted as additional parameter
        /// to service and then received along with main answer.
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// Access token received from service. Can be used for further service API calls.
        /// </summary>
        public NameValueCollection AccessToken { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthClient" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        protected OAuthClient(IRequestFactory factory, IClientConfiguration configuration)
        {
            _factory = factory;
            _configuration = configuration;
        }

        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process.
        /// You should use this URI when rendering login link.
        /// </summary>
        /// <param name="state">Any additional information needed by application.</param>
        /// <returns>Login link URI.</returns>
        public string GetLoginLinkUri(string state = null)
        {
            return GetLoginRequestUri(GetRequestToken(), state);
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

            AccessToken = GetAccessToken(token, verifier);

            return QueryUserInfo();
        }

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
        /// Should return parsed <see cref="UserInfo"/> using content of callback issued by service.
        /// </summary>
        protected abstract UserInfo ParseUserInfo(string content);

        /// <summary>
        /// Issues request for request token and returns result.
        /// </summary>
        private NameValueCollection GetRequestToken()
        {
            var client = _factory.CreateClient();
            client.BaseUrl = RequestTokenServiceEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForRequestToken(
                _configuration.ClientId, _configuration.ClientSecret, _configuration.RedirectUri);

            var request = _factory.CreateRequest();
            request.Resource = RequestTokenServiceEndpoint.Resource;
            request.Method = Method.POST;

            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(response.Content))
            {
                throw new UnexpectedResponseException(response);
            }

            return HttpUtility.ParseQueryString(response.Content);
        }

        /// <summary>
        /// Composes login link URI.
        /// </summary>
        /// <param name="response">Content of response for request token request.</param>
        /// <param name="state">Any additional information needed by application.</param>
        private string GetLoginRequestUri(NameValueCollection response, string state = null)
        {
            var client = _factory.CreateClient();
            client.BaseUrl = LoginServiceEndpoint.BaseUri;

            var request = _factory.CreateRequest();
            request.Resource = LoginServiceEndpoint.Resource;
            var tokenKey = response[OAuthTokenKey];

            if (string.IsNullOrWhiteSpace(tokenKey))
            {
                throw new UnexpectedResponseException(OAuthTokenKey);
            }

            request.AddParameter(OAuthTokenKey, tokenKey);
            if (!state.IsEmpty())
            {
                request.AddParameter("state", state);
            }
            
            _secret = response[OAuthTokenSecretKey];            
            if (string.IsNullOrWhiteSpace(_secret))
            {
                throw new UnexpectedResponseException(OAuthTokenSecretKey);
            }

            return client.BuildUri(request).ToString();
        }

        /// <summary>
        /// Obtains access token by calling corresponding service.
        /// </summary>
        /// <param name="token">Token posted with callback issued by provider.</param>
        /// <param name="verifier">Verifier posted with callback issued by provider.</param>
        /// <returns>Access token and other extra info.</returns>
        private NameValueCollection GetAccessToken(string token, string verifier)
        {
            var client = _factory.CreateClient();
            client.BaseUrl = AccessTokenServiceEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForAccessToken(
                _configuration.ClientId, _configuration.ClientSecret, token, _secret, verifier);

            var request = _factory.CreateRequest();
            request.Resource = AccessTokenServiceEndpoint.Resource;
            request.Method = Method.POST;

            var response = client.Execute(request);
            return HttpUtility.ParseQueryString(response.Content);
        }

        /// <summary>
        /// Queries user info using corresponding service and data received by access token request.
        /// </summary>
        private UserInfo QueryUserInfo()
        {
            var token = AccessToken[OAuthTokenKey];
            var secret = AccessToken[OAuthTokenSecretKey];

            var client = _factory.CreateClient();
            client.BaseUrl = UserInfoServiceEndpoint.BaseUri;
            client.Authenticator = OAuth1Authenticator.ForProtectedResource(
                _configuration.ClientId, _configuration.ClientSecret, token, secret);

            var request = _factory.CreateRequest();
            request.Resource = UserInfoServiceEndpoint.Resource;

            var response = client.Execute(request);

            var result = ParseUserInfo(response.Content);
            result.ProviderName = Name;

            return result;
        }
    }
}