using System;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client
{
    /// <summary>
    /// Base class for any OAuth2 client implementation within this library.
    /// Essentially descendants of this class are intended for doing user authentication using
    /// certain third-party service.
    /// </summary>
    /// <remarks>
    /// Standard flow is:
    /// - client instance generates URI for login link
    /// - hosting app renders page with login link using aforementioned URI
    /// - user clicks login link - this leads to redirect to third-party service site
    /// - user does authentication and allows app access his/her basic information
    /// - third-party service redirects user to hosting app
    /// - hosting app reads user information using <see cref="GetUserInfo"/> method on callback
    /// </remarks>
    public abstract class OAuth2Client : IClient
    {
        private readonly IRequestFactory factory;
        private readonly IClientConfiguration configuration;

        /// <summary>
        /// Defines URI of service which issues access code.
        /// </summary>
        protected abstract Endpoint AccessCodeServiceEndpoint { get; }

        /// <summary>
        /// Defines URI of service which issues access token.
        /// </summary>
        protected abstract Endpoint AccessTokenServiceEndpoint { get; }

        /// <summary>
        /// Defines URI of service which allows to obtain information about user which is currently logged in.
        /// </summary>
        protected abstract Endpoint UserInfoServiceEndpoint { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2Client"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        protected OAuth2Client(IRequestFactory factory, IConfigurationManager configurationManager)
        {
            this.factory = factory;
            configuration = configurationManager
                .GetConfigSection<OAuth2ConfigurationSection>("oauth2")
                [GetType().Name];
        }

        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process.
        /// You should use this URI when rendering login link.
        /// </summary>
        public string GetLoginLinkUri()
        {
            var client = factory.NewClient();
            client.BaseUrl = AccessCodeServiceEndpoint.BaseUri;

            var request = factory.NewRequest();
            request.Resource = AccessCodeServiceEndpoint.Resource;

            request.AddObject(new
            {
                response_type = "code",
                client_id = configuration.ClientId,
                redirect_uri = configuration.RedirectUri,
                scope = configuration.Scope
            });

            return client.BuildUri(request).ToString();
        }

        /// <summary>
        /// Obtains user information using third-party authentication service
        /// using data provided via callback request.
        /// </summary>
        /// <param name="parameters">Callback request payload (parameters).</param>
        public UserInfo GetUserInfo(NameValueCollection parameters)
        {
            var error = parameters["error"];
            if (!error.IsEmpty())
            {
                throw new ApplicationException(error);
            }

            var client = factory.NewClient();
            client.BaseUrl = AccessTokenServiceEndpoint.BaseUri;
            
            var request = factory.NewRequest();
            request.Resource = AccessTokenServiceEndpoint.Resource;
            request.Method = Method.POST;
            request.AddObject(new
            {
                code = parameters["code"],
                client_id = configuration.ClientId,
                client_secret = configuration.ClientSecret,
                redirect_uri = configuration.RedirectUri,
                grant_type = "authorization_code"
            });

            var response = client.Execute(request);
            AfterGetAccessToken(response);

            var content = response.Content;
            try
            {
                // response can be sent in JSON format
                return GetUserInfo((string) JObject.Parse(content).SelectToken("access_token"));
            }
            catch (JsonReaderException)
            {
                // or it can be in "query string" format (param1=val1&param2=val2)
                return GetUserInfo(content.ToDictionary()["access_token"]);
            }
        }

        /// <summary>
        /// Obtains user information using third-party authentication service.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        private UserInfo GetUserInfo(string accessToken)
        {
            var client = factory.NewClient();
            client.BaseUrl = UserInfoServiceEndpoint.BaseUri;
            client.Authenticator = new OAuth2UriQueryParameterAuthenticator(accessToken);

            var request = factory.NewRequest();
            request.Resource = UserInfoServiceEndpoint.Resource;
            
            OnGetUserInfo(request);
            return ParseUserInfo(client.Execute(request).Content);
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected virtual void OnGetUserInfo(IRestRequest request)
        {
        }

        /// <summary>
        /// Called just after obtaining response with access token from third-party service.
        /// Allows to read extra data returned along with access token.
        /// </summary>
        protected virtual void AfterGetAccessToken(IRestResponse response)
        {
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected abstract UserInfo ParseUserInfo(string content);
    }
}