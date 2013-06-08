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
            var client = _factory.CreateClient(AccessCodeServiceEndpoint);
            var request = _factory.CreateRequest(AccessCodeServiceEndpoint);
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

        /// <summary>
        /// Obtains user information using OAuth2 service and
        /// data provided via callback request.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        public UserInfo GetUserInfo(NameValueCollection parameters)
        {
            const string errorFieldName = "error";
            var error = parameters[errorFieldName];
            if (!error.IsEmpty())
            {
                throw new UnexpectedResponseException(errorFieldName);
            }

            State = parameters["state"];

            QueryAccessToken(parameters);
            return GetUserInfo();
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

        /// <summary>
        /// Issues query for access token and parses response.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        private void QueryAccessToken(NameValueCollection parameters)
        {
            var client = _factory.CreateClient(AccessTokenServiceEndpoint);
            var request = _factory.CreateRequest(AccessTokenServiceEndpoint, Method.POST);

            BeforeGetAccessToken(new BeforeAfterRequestArgs
            {
                Client = client,
                Request = request,
                Parameters = parameters,
                Configuration = _configuration
            });

            var response = client.ExecuteAndVerify(request);

            AfterGetAccessToken(new BeforeAfterRequestArgs
            {
                Response = response,
                Parameters = parameters
            });

            AccessToken = ParseAccessTokenResponse(response.Content);
        }

        protected virtual string ParseAccessTokenResponse(string content)
        {
            try
            {
                // response can be sent in JSON format
                var token = (string)JObject.Parse(content).SelectToken(AccessTokenKey);
                if (token.IsEmpty())
                {
                    throw new UnexpectedResponseException(AccessTokenKey);
                }
                return token;
            }
            catch (JsonReaderException)
            {
                // or it can be in "query string" format (param1=val1&param2=val2)
                var collection = HttpUtility.ParseQueryString(content);
                return collection.GetOrThrowUnexpectedResponse(AccessTokenKey);
            }
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
        protected virtual void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
        }

        /// <summary>
        /// Called just after obtaining response with access token from service.
        /// Allows to read extra data returned along with access token.
        /// </summary>
        protected virtual void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
        }

        protected virtual void BeforeGetAccessToken(BeforeAfterRequestArgs args)
        {
            args.Request.AddObject(new
            {
                code = args.Parameters.GetOrThrowUnexpectedResponse("code"),
                client_id = _configuration.ClientId,
                client_secret = _configuration.ClientSecret,
                redirect_uri = _configuration.RedirectUri,
                grant_type = "authorization_code"
            });
        }

        /// <summary>
        /// Obtains user information using provider API.
        /// </summary>
        private UserInfo GetUserInfo()
        {
            var client = _factory.CreateClient(UserInfoServiceEndpoint);
            client.Authenticator = new OAuth2UriQueryParameterAuthenticator(AccessToken);
            var request = _factory.CreateRequest(UserInfoServiceEndpoint);

            BeforeGetUserInfo(new BeforeAfterRequestArgs
            {
                Client = client,
                Request = request,
                Configuration = _configuration
            });

            var response = client.ExecuteAndVerify(request);

            var result = ParseUserInfo(response.Content);
            result.ProviderName = Name;

            return result;
        }

        public class BeforeAfterRequestArgs
        {
            /// <summary>
            /// Client instance.
            /// </summary>
            public IRestClient Client { get; set; }

            /// <summary>
            /// Request instance.
            /// </summary>
            public IRestRequest Request { get; set; }

            /// <summary>
            /// Response instance.
            /// </summary>
            public IRestResponse Response { get; set; }

            /// <summary>
            /// Values received from service.
            /// </summary>
            public NameValueCollection Parameters { get; set; }

            /// <summary>
            /// Client configuration.
            /// </summary>
            public IClientConfiguration Configuration { get; set; }
        }
    }
}