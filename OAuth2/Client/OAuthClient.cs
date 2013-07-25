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
    /// <summary>
    /// Base class for OAuth (version 1) client implementation.
    /// </summary>
    public abstract class OAuthClient : IClient
    {
        private const string OAuthTokenKey = "oauth_token";
        private const string OAuthTokenSecretKey = "oauth_token_secret";

        private readonly IRequestFactory _factory;

        /// <summary>
        /// Client configuration object.
        /// </summary>
        public IClientConfiguration Configuration { get; private set; }

        /// <summary>
        /// Friendly name of provider (OAuth service).
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// State which was posted as additional parameter
        /// to service and then received along with main answer.
        /// </summary>
        public string State { get { return null; } }

        /// <summary>
        /// Access token received from service. Can be used for further service API calls.
        /// </summary>
        public string AccessToken { get; private set; }

        /// <summary>
        /// Access token secret received from service. Can be used for further service API calls.
        /// </summary>
        public string AccessTokenSecret { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthClient" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        protected OAuthClient(IRequestFactory factory, IClientConfiguration configuration)
        {
            _factory = factory;
            Configuration = configuration;
        }

        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process.
        /// You should use this URI when rendering login link.
        /// </summary>
        /// <param name="state">Any additional information needed by application.</param>
        /// <returns>Login link URI.</returns>
        public string GetLoginLinkUri(string state = null)
        {
            if (!state.IsEmpty())
            {
                throw new NotSupportedException("State transmission is not supported by current implementation.");
            }
            QueryRequestToken();
            return GetLoginRequestUri(state);
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
            AccessToken = parameters.GetOrThrowUnexpectedResponse(OAuthTokenKey);
            QueryAccessToken(parameters.GetOrThrowUnexpectedResponse("oauth_verifier"));

            var result = ParseUserInfo(QueryUserInfo());
            result.ProviderName = Name;

            return result;
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
        private void QueryRequestToken()
        {
            var client = _factory.CreateClient(RequestTokenServiceEndpoint);
            client.Authenticator = OAuth1Authenticator.ForRequestToken(
                Configuration.ClientId, Configuration.ClientSecret, Configuration.RedirectUri);
            
            var request = _factory.CreateRequest(RequestTokenServiceEndpoint, Method.POST);
            
            var response = client.ExecuteAndVerify(request);
            var collection = HttpUtility.ParseQueryString(response.Content);

            AccessToken = collection.GetOrThrowUnexpectedResponse(OAuthTokenKey);
            AccessTokenSecret = collection.GetOrThrowUnexpectedResponse(OAuthTokenSecretKey);
        }

        /// <summary>
        /// Composes login link URI.
        /// </summary>
        /// <param name="state">Any additional information needed by application.</param>
        private string GetLoginRequestUri(string state = null)
        {
            var client = _factory.CreateClient(LoginServiceEndpoint);
            var request = _factory.CreateRequest(LoginServiceEndpoint);

            request.AddParameter(OAuthTokenKey, AccessToken);
            if (!state.IsEmpty())
            {
                request.AddParameter("state", state);
            }

            return client.BuildUri(request).ToString();
        }

        /// <summary>
        /// Obtains access token by calling corresponding service.
        /// </summary>
        /// <param name="verifier">Verifier posted with callback issued by provider.</param>
        /// <returns>Access token and other extra info.</returns>
        private void QueryAccessToken(string verifier)
        {
            var client = _factory.CreateClient(AccessTokenServiceEndpoint);
            client.Authenticator = OAuth1Authenticator.ForAccessToken(
                Configuration.ClientId, Configuration.ClientSecret, AccessToken, AccessTokenSecret, verifier);

            var request = _factory.CreateRequest(AccessTokenServiceEndpoint, Method.POST);

            var content = client.ExecuteAndVerify(request).Content;
            var collection = HttpUtility.ParseQueryString(content);
            
            AccessToken = collection.GetOrThrowUnexpectedResponse(OAuthTokenKey);
            AccessTokenSecret = collection.GetOrThrowUnexpectedResponse(OAuthTokenSecretKey);
        }

        /// <summary>
        /// Queries user info using corresponding service and data received by access token request.
        /// </summary>
        private string QueryUserInfo()
        {
            var client = _factory.CreateClient(UserInfoServiceEndpoint);
            client.Authenticator = OAuth1Authenticator.ForProtectedResource(
                Configuration.ClientId, Configuration.ClientSecret, AccessToken, AccessTokenSecret);

            var request = _factory.CreateRequest(UserInfoServiceEndpoint);
            
            return client.ExecuteAndVerify(request).Content;
        }
    }
}