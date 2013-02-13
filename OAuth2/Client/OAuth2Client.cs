using System;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using RestSharp.Contrib;

namespace OAuth2.Client
{
    /// <summary>
    /// Base class for OAuth2 client implementation.
    /// </summary>
    public abstract class OAuth2Client : IClient
    {
        private const string AccessTokenKey = "access_token";

        private readonly IRequestFactory _factory;
        private readonly IClientConfiguration _configuration;

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public abstract string ProviderName { get; }
        
        /// <summary>
        /// Access token returned by provider. Can be used for further calls of provider API.
        /// </summary>
        public object AccessToken { get; private set; }

        /// <summary>
        /// State (any additional information that was provided by application and is posted back by service).
        /// </summary>
        public string State { get; private set; }

        /// <summary>
        /// Error (if any).
        /// </summary>
        public string Error { get; private set; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2Client"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        protected OAuth2Client(IRequestFactory factory, IClientConfiguration configuration)
        {
            _factory = factory;
            _configuration = configuration;
        }

        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process.
        /// This URI should be used for rendering login link.
        /// </summary>
        /// <remarks>
        /// Any additional information that will be posted back by service.
        /// </remarks>
        public string GetLoginLinkUri(string state = null)
        {            
            var client = _factory.NewClient();
            client.BaseUrl = AccessCodeServiceEndpoint.BaseUri;

            var request = _factory.NewRequest();
            request.Resource = AccessCodeServiceEndpoint.Resource;

            request.AddObject(new
            {
                response_type = "code",
                client_id = _configuration.ClientId,
                redirect_uri = _configuration.RedirectUri,
                scope = _configuration.Scope,
                state
            });

            return client.BuildUri(request).ToString();
        }

        public virtual void Finalize(NameValueCollection parameters)
        {
            this.AccessToken = null;
            
            if (!parameters["error"].IsEmpty())
                this.Error = parameters["error"];
            if(!parameters["state"].IsEmpty())
                this.State = parameters["state"];
            
            if (!this.Error.IsEmpty())            
                throw new ApplicationException(this.Error);

            this.AccessToken = this.GetAccessToken(parameters);
        }

        /// <summary>
        /// Obtains user information using OAuth2 service and
        /// data provided via callback request.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        public UserInfo GetUserInfo(NameValueCollection parameters)
        {
            this.Finalize(parameters);
            return this.GetUserInfo(this.AccessToken as string);
        }

        protected virtual dynamic BuildAccessTokenExchangeObject(NameValueCollection parameters, IClientConfiguration configuration)
        {
            return new
            {
                code = parameters["code"],
                client_id = configuration.ClientId,
                client_secret = configuration.ClientSecret,
                redirect_uri = configuration.RedirectUri,
                grant_type = "authorization_code"
            };
        }

        /// <summary>
        /// Issues query for access token and parses response.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        private string GetAccessToken(NameValueCollection parameters)
        {
            var client = _factory.NewClient();
            client.BaseUrl = AccessTokenServiceEndpoint.BaseUri;

            var request = _factory.NewRequest();
            request.Resource = AccessTokenServiceEndpoint.Resource;
            request.Method = Method.POST;
            request.AddObject(this.BuildAccessTokenExchangeObject(parameters, _configuration));

            var response = client.Execute(request);
            AfterGetAccessToken(response);

            var content = response.Content;
            try
            {
                // response can be sent in JSON format
                return (string) JObject.Parse(content).SelectToken(AccessTokenKey);
            }
            catch (JsonReaderException)
            {
                // or it can be in "query string" format (param1=val1&param2=val2)
                return HttpUtility.ParseQueryString(content)[AccessTokenKey];
            }
        }

        /// <summary>
        /// Obtains user information using provider API.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        private UserInfo GetUserInfo(string accessToken)
        {
            var client = _factory.NewClient();
            client.BaseUrl = UserInfoServiceEndpoint.BaseUri;
            client.Authenticator = new OAuth2UriQueryParameterAuthenticator(accessToken);

            var request = _factory.NewRequest();
            request.Resource = UserInfoServiceEndpoint.Resource;

            BeforeGetUserInfo(request);

            var result = ParseUserInfo(client.Execute(request).Content);
            result.ProviderName = ProviderName;

            return result;
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> using content received from provider.
        /// </summary>
        /// <param name="content">The content which is received from provider.</param>
        protected abstract UserInfo ParseUserInfo(string content);

        /// <summary>
        /// Called just before issuing request to service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected virtual void BeforeGetUserInfo(IRestRequest request)
        {
        }

        /// <summary>
        /// Called just after obtaining response with access token from service.
        /// Allows to read extra data returned along with access token.
        /// </summary>
        protected virtual void AfterGetAccessToken(IRestResponse response)
        {
        }
    }
}