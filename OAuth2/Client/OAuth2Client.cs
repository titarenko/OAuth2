using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Portable;
using System.Net.Http;
using RestSharp.Portable.Authenticators;
using System.Threading.Tasks;
using System.Linq;
using System;

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

        private const string GrantTypeAuthorizationKey = "authorization_code";
        private const string GrantTypeRefreshTokenKey = "refresh_token";

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
        public string AccessToken { get; protected set; }

        /// <summary>
        /// Refresh token returned by provider. Can be used for further calls of provider API.
        /// </summary>
        public string RefreshToken { get; protected set; }
        
        /// <summary>
        /// Token type returned by provider. Can be used for further calls of provider API.
        /// </summary>
        public string TokenType { get; private set; }
        
        /// <summary>
        /// The time when the access token expires
        /// </summary>
        public DateTime? ExpiresAt { get; private set; }

        /// <summary>
        /// A safety margin that's used to see if an access token is expired
        /// </summary>
        public TimeSpan ExpirationSafetyMargin { get; set; }
        
        private string GrantType { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2Client"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        protected OAuth2Client(IRequestFactory factory, IClientConfiguration configuration)
        {
            ExpirationSafetyMargin = TimeSpan.FromSeconds(5);
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
        public virtual async Task<string> GetLoginLinkUri(string state = null)
        {
            var client = _factory.CreateClient(AccessCodeServiceEndpoint);
            var request = _factory.CreateRequest(AccessCodeServiceEndpoint);
            request.AddObject(new
            {
                response_type = "code",
                client_id = Configuration.ClientId,
                redirect_uri = Configuration.RedirectUri,
                scope = Configuration.Scope,
                state
            }, new string[] { (string.IsNullOrEmpty(Configuration.Scope) ? "scope" : null) }, PropertyFilterMode.Exclude);
            await BeforeGetLoginLinkUri(new BeforeAfterRequestArgs()
            {
                Client = client,
                Request = request,
                Configuration = Configuration,
            });
            return await Task<string>.Factory.StartNew(() => client.BuildUrl(request).ToString());
        }

        /// <summary>
        /// Obtains user information using OAuth2 service and data provided via callback request.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        public async Task<UserInfo> GetUserInfo(ILookup<string, string> parameters)
        {
            GrantType = GrantTypeAuthorizationKey;
            CheckErrorAndSetState(parameters);
            await QueryAccessToken(parameters);
            return await GetUserInfo();
        }

        /// <summary>
        /// Issues query for access token and returns access token.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        public async Task<string> GetToken(ILookup<string, string> parameters)
        {
            GrantType = GrantTypeAuthorizationKey;
            CheckErrorAndSetState(parameters);
            await QueryAccessToken(parameters);
            return AccessToken;
        }

        /// <summary>
        /// Get the current access token - and optinally refreshes it if it is expired
        /// </summary>
        /// <param name="refreshToken">The refresh token to use (null == default)</param>
        /// <param name="forceUpdate">Enfore an update of the access token?</param>
        /// <param name="safetyMargin">A custom safety margin to check if the access token is expired</param>
        /// <returns></returns>
        public async Task<string> GetCurrentToken(string refreshToken = null, bool forceUpdate = false, TimeSpan? safetyMargin = null)
        {
            bool refreshRequired =
                forceUpdate
                || (ExpiresAt != null && DateTime.Now >= (ExpiresAt - (safetyMargin ?? ExpirationSafetyMargin))) 
                || String.IsNullOrEmpty(AccessToken);

            if (refreshRequired)
            {
                var parameters = new Dictionary<string, string>();
                if (!String.IsNullOrEmpty(refreshToken))
                {
                    parameters.Add(RefreshTokenKey, refreshToken);
                }
                else if (!String.IsNullOrEmpty(RefreshToken))
                {
                    parameters.Add(RefreshTokenKey, RefreshToken);
                }
                if (parameters.Count > 0)
                {
                    GrantType = GrantTypeRefreshTokenKey;
                    await QueryAccessToken(parameters.ToLookup(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase));
                    return AccessToken;
                }
                throw new Exception("Token never fetched and refresh token not provided.");
            }

            return AccessToken;
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

        private void CheckErrorAndSetState(ILookup<string, string> parameters)
        {
            const string errorFieldName = "error";

            var error = parameters[errorFieldName].ToList();
            if (error.Any(x => !string.IsNullOrEmpty(x)))
                throw new UnexpectedResponseException(errorFieldName, string.Join("\n", error));

            State = string.Join(",", parameters["state"]);
        }

        /// <summary>
        /// Issues query for access token and parses response.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        private async Task QueryAccessToken(ILookup<string, string> parameters)
        {
            var client = _factory.CreateClient(AccessTokenServiceEndpoint);
            var request = _factory.CreateRequest(AccessTokenServiceEndpoint, HttpMethod.Post);

            BeforeGetAccessToken(new BeforeAfterRequestArgs
            {
                Client = client,
                Request = request,
                Parameters = parameters,
                Configuration = Configuration
            });

            var response = await client.ExecuteAndVerify(request);

            var content = response.GetContent();
            AccessToken = ParseAccessTokenResponse(content);

            if (GrantType != GrantTypeRefreshTokenKey)
                RefreshToken = ParseStringResponse(content, new[] { RefreshTokenKey })[RefreshTokenKey].FirstOrDefault();
            TokenType = ParseStringResponse(content, new[] { TokenTypeKey })[TokenTypeKey].FirstOrDefault();

            var expiresIn = ParseStringResponse(content, new[] { ExpiresKey })[ExpiresKey].Select(x => Convert.ToInt32(x, 10)).FirstOrDefault();
            ExpiresAt = (expiresIn != 0 ? (DateTime?)DateTime.Now.AddSeconds(expiresIn) : null);

            AfterGetAccessToken(new BeforeAfterRequestArgs
            {
                Response = response,
                Parameters = parameters
            });
        }

        /// <summary>
        /// Parse the access token response using either JSON or form url encoded parameters
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        protected virtual string ParseAccessTokenResponse(string content)
        {
            return ParseStringResponse(content, AccessTokenKey);
        }

        protected static string ParseStringResponse(string content, string key)
        {
            var values = ParseStringResponse(content, new[] { key })[key].ToList();
            if (values.Count == 0)
                throw new UnexpectedResponseException(key);
            return values.First();
        }

        /// <summary>
        /// Parse the response for a given key/value using either JSON or form url encoded parameters
        /// </summary>
        /// <param name="content"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        protected static ILookup<string, string> ParseStringResponse(string content, params string[] keys)
        {
            var result = new List<KeyValuePair<string, string>>();
            try
            {
                // response can be sent in JSON format
                var jobj = JObject.Parse(content);
                foreach (var key in keys)
                {
                    foreach (var token in jobj.SelectTokens(key))
                        if (token.HasValues)
                        {
                            foreach (var value in token.Values())
                                result.Add(new KeyValuePair<string, string>(key, (string)value));
                        }
                        else
                            result.Add(new KeyValuePair<string, string>(key, (string)token));
                }
            }
            catch (JsonReaderException)
            {
                // or it can be in "query string" format (param1=val1&param2=val2)
                var collection = content.ParseQueryString();
                foreach (var key in keys)
                {
                    foreach (var item in collection[key])
                        result.Add(new KeyValuePair<string, string>(key, item));
                }
            }
            return result.ToLookup(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> using content received from provider.
        /// </summary>
        /// <param name="content">The content which is received from provider.</param>
        protected abstract UserInfo ParseUserInfo(string content);

        /// <summary>
        /// Called just before building the request URI when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected virtual async Task BeforeGetLoginLinkUri(BeforeAfterRequestArgs args)
        {
            await Task.Factory.StartNew(() => { });
        }

        /// <summary>
        /// Called before the request to get the access token
        /// </summary>
        /// <param name="args"></param>
        protected virtual void BeforeGetAccessToken(BeforeAfterRequestArgs args)
        {
            args.Request.AddObject(new
            {
                client_id = Configuration.ClientId,
                client_secret = Configuration.ClientSecret,
                grant_type = GrantType
            });
            if (GrantType == GrantTypeRefreshTokenKey)
            {
                args.Request.AddObject(new
                {
                    refresh_token = args.Parameters.GetOrThrowUnexpectedResponse(RefreshTokenKey),
                });
            }
            else
            {
                args.Request.AddObject(new
                {
                    code = args.Parameters.GetOrThrowUnexpectedResponse("code"),
                    redirect_uri = Configuration.RedirectUri,
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
        protected virtual async Task<UserInfo> GetUserInfo()
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

            var response = await client.ExecuteAndVerify(request);

            var result = ParseUserInfo(response.GetContent());
            result.ProviderName = Name;

            return result;
        }
    }
}