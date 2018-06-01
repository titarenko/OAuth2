using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace OAuth2.Client
{
    /// <summary>
    /// Base class for OAuth2 client implementation.
    /// </summary>
    public abstract class OAuth2Client : IClient
    {
        private const string AccessTokenKey = "access_token";
        private const string RefreshTokenKey = "refresh_token";
        private const string ExpiresKey = "expires_in";
        private const string TokenTypeKey = "token_type";

        private readonly IRequestFactory _factory;

        /// <summary>
        /// Client configuration object.
        /// </summary>
        public IClientConfiguration Configuration { get; private set; }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// State (any additional information that was provided by application and is posted back by service).
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// Access token returned by provider. Can be used for further calls of provider API.
        /// </summary>
        public string AccessToken { get; private set; }

        /// <summary>
        /// Refresh token returned by provider. Can be used for further calls of provider API.
        /// </summary>
        public string RefreshToken { get; private set; }

        /// <summary>
        /// Token type returned by provider. Can be used for further calls of provider API.
        /// </summary>
        public string TokenType { get; private set; }

        /// <summary>
        /// Seconds till the token expires returned by provider. Can be used for further calls of provider API.
        /// </summary>
        public DateTime ExpiresAt { get; private set; }

        private string GrantType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2Client"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        protected OAuth2Client(IRequestFactory factory, IClientConfiguration configuration)
        {
            _factory = factory;
            Configuration = configuration;
        }

        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process.
        /// This URI should be used for rendering login link.
        /// </summary>
        /// <param name="state">
        /// Any additional information that will be posted back by service.
        /// </param>
        /// <param name="cancellationToken"></param>
        public virtual Task<string> GetLoginLinkUriAsync(string state = null, CancellationToken cancellationToken = default)
        {
            var client = _factory.CreateClient(AccessCodeServiceEndpoint);
            var request = _factory.CreateRequest(AccessCodeServiceEndpoint);
            if (String.IsNullOrEmpty(Configuration.Scope))
            {
                request.AddObject(new
                {
                    response_type = "code",
                    client_id = Configuration.ClientId,
                    redirect_uri = Configuration.RedirectUri,
                    state
                });
            }
            else
            {
                request.AddObject(new
                {
                    response_type = "code",
                    client_id = Configuration.ClientId,
                    redirect_uri = Configuration.RedirectUri,
                    scope = Configuration.Scope,
                    state
                });
            }
            return Task.FromResult(client.BuildUri(request).ToString());
        }

        /// <summary>
        /// Issues query for access token and returns access token.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public async Task<string> GetTokenAsync(NameValueCollection parameters, CancellationToken cancellationToken = default)
        {
            GrantType = "authorization_code";
            CheckErrorAndSetState(parameters);
            await QueryAccessTokenAsync(parameters, cancellationToken);
            return AccessToken;
        }

        public async Task<string> GetCurrentTokenAsync(string refreshToken = null, bool forceUpdate = false, CancellationToken cancellationToken = default)
        {
            if (!forceUpdate && ExpiresAt != default && DateTime.Now < ExpiresAt && !String.IsNullOrEmpty(AccessToken))
            {
                return AccessToken;
            }

            NameValueCollection parameters = new NameValueCollection();
            if (!String.IsNullOrEmpty(refreshToken))
            {
                parameters.Add("refresh_token", refreshToken);
            }
            else if (!String.IsNullOrEmpty(RefreshToken))
            {
                parameters.Add("refresh_token", RefreshToken);
            }

            if (parameters.Count > 0)
            {
                GrantType = "refresh_token";
                await QueryAccessTokenAsync(parameters, cancellationToken).ConfigureAwait(false);
                return AccessToken;
            }
            throw new Exception("Token never fetched and refresh token not provided.");
        }

        /// <summary>
        /// Defines URI of service which issues access code.
        /// </summary>
        protected abstract Endpoint AccessCodeServiceEndpoint { get; }

        /// <summary>
        /// Defines URI of service which issues access token.
        /// </summary>
        protected abstract Endpoint AccessTokenServiceEndpoint { get; }

        /// <summary>
        /// Defines URI of service which allows to obtain information about user 
        /// who is currently logged in.
        /// </summary>
        protected abstract Endpoint UserInfoServiceEndpoint { get; }

        private void CheckErrorAndSetState(NameValueCollection parameters)
        {
            const string errorFieldName = "error";
            var error = parameters[errorFieldName];
            if (!error.IsEmpty())
            {
                throw new UnexpectedResponseException(errorFieldName);
            }

            State = parameters["state"];
        }

        /// <summary>
        /// Issues query for access token and parses response.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        private async Task QueryAccessTokenAsync(NameValueCollection parameters, CancellationToken cancellationToken = default)
        {
            var client = _factory.CreateClient(AccessTokenServiceEndpoint);
            var request = _factory.CreateRequest(AccessTokenServiceEndpoint, Method.POST);

            BeforeGetAccessToken(new BeforeAfterRequestArgs
            {
                Client = client,
                Request = request,
                Parameters = parameters,
                Configuration = Configuration
            });

            var response = await client.ExecuteAndVerifyAsync(request, cancellationToken).ConfigureAwait(false);

            AfterGetAccessToken(new BeforeAfterRequestArgs
            {
                Response = response,
                Parameters = parameters
            });

            AccessToken = ParseTokenResponse(response.Content, AccessTokenKey);
            if (String.IsNullOrEmpty(AccessToken))
                throw new UnexpectedResponseException(AccessTokenKey);

            if (GrantType != "refresh_token")
                RefreshToken = ParseTokenResponse(response.Content, RefreshTokenKey);

            TokenType = ParseTokenResponse(response.Content, TokenTypeKey);

            if (Int32.TryParse(ParseTokenResponse(response.Content, ExpiresKey), out int expiresIn))
                ExpiresAt = DateTime.Now.AddSeconds(expiresIn);
        }

        protected virtual string ParseTokenResponse(string content, string key)
        {
            if (String.IsNullOrEmpty(content) || String.IsNullOrEmpty(key))
                return null;

            try
            {
                // response can be sent in JSON format
                var token = JObject.Parse(content).SelectToken(key);
                return token?.ToString();
            }
            catch (JsonReaderException)
            {
                // or it can be in "query string" format (param1=val1&param2=val2)
                var collection = HttpUtility.ParseQueryString(content);
                return collection[key];
            }
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> using content received from provider.
        /// </summary>
        /// <param name="content">The content which is received from provider.</param>
        protected abstract UserInfo ParseUserInfo(string content);

        protected virtual void BeforeGetAccessToken(BeforeAfterRequestArgs args)
        {
            if (GrantType == "refresh_token")
            {
                args.Request.AddObject(new
                {
                    refresh_token = args.Parameters.GetOrThrowUnexpectedResponse("refresh_token"),
                    client_id = Configuration.ClientId,
                    client_secret = Configuration.ClientSecret,
                    grant_type = GrantType
                });
            }
            else
            {
                args.Request.AddObject(new
                {
                    code = args.Parameters.GetOrThrowUnexpectedResponse("code"),
                    client_id = Configuration.ClientId,
                    client_secret = Configuration.ClientSecret,
                    redirect_uri = Configuration.RedirectUri,
                    grant_type = GrantType
                });
            }
        }

        /// <summary>
        /// Called just after obtaining response with access token from service.
        /// Allows to read extra data returned along with access token.
        /// </summary>
        protected virtual void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
        }

        /// <summary>
        /// Called just before issuing request to service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected virtual void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
        }

        /// <summary>
        /// Obtains user information using provider API.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellationtoken</param>
        protected virtual async Task<UserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            var client = _factory.CreateClient(UserInfoServiceEndpoint);
            client.Authenticator = new OAuth2UriQueryParameterAuthenticator(AccessToken);
            var request = _factory.CreateRequest(UserInfoServiceEndpoint);

            BeforeGetUserInfo(new BeforeAfterRequestArgs
            {
                Client = client,
                Request = request,
                Configuration = Configuration
            });

            var response = await client.ExecuteAndVerifyAsync(request, cancellationToken).ConfigureAwait(false);

            var result = ParseUserInfo(response.Content);
            result.ProviderName = Name;

            return result;
        }

        /// <inheritdoc />
        public async Task<UserInfo> GetUserInfoAsync(NameValueCollection parameters, CancellationToken cancellationToken = default)
        {
            GrantType = "authorization_code";
            CheckErrorAndSetState(parameters);
            await QueryAccessTokenAsync(parameters, cancellationToken).ConfigureAwait(false);
            return await GetUserInfoAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}