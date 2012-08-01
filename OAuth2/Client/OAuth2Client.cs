using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2.Parameters;
using RestSharp;
using System.Linq;

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
        private readonly IRestClient client;
        private readonly IRestRequest request;
        private readonly IConfiguration configuration;

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
        /// <param name="client">The client.</param>
        /// <param name="request">The request.</param>
        /// <param name="configuration">The configuration.</param>
        protected OAuth2Client(IRestClient client, IRestRequest request, IConfiguration configuration)
        {
            this.client = client;
            this.request = request;
            this.configuration = configuration.GetSection(GetType());
        }

        /// <summary>
        /// Returns URI of service which should be called in order to start authentication process. 
        /// You should use this URI when rendering login link.
        /// </summary>
        public string GetAccessCodeRequestUri()
        {
            return "{0}?{1}".Fill(
                AccessCodeServiceEndpoint.Uri,
                configuration.Get<AccessCodeRequestParameters>().ToQueryString());
        }

        /// <summary>
        /// Returns access token using given code by querying corresponding service.
        /// </summary>
        /// <param name="code">The code which was obtained from third-party authentication service.</param>
        /// <param name="error">The error which was received from third-party authentication service.</param>
        public string GetAccessToken(string code, string error)
        {
            if (!error.IsEmpty())
            {
                throw new ApplicationException(error);
            }

            client.BaseUrl = AccessTokenServiceEndpoint.BaseUri;
            request.Resource = AccessTokenServiceEndpoint.Resource;
            request.Method = Method.POST;

            var parameters = configuration.Get<AccessTokenRequestParameters>();
            parameters.Code = code;

            request.AddObjectPropertiesAsParameters(parameters);
            var response = client.Execute(request);
            AfterGetAccessToken(response);

            var content = response.Content;
            try
            {
                // response can be sent in JSON format
                return (string)JObject.Parse(content).SelectToken("access_token");
            }
            catch (JsonReaderException)
            {
                // or it can be in "query string" format (param1=val1&param2=val2)
                return content.ToDictionary()["access_token"];
            }
        }

        /// <summary>
        /// Obtains user information using third-party authentication service.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public UserInfo GetUserInfo(string accessToken)
        {
            client.BaseUrl = UserInfoServiceEndpoint.BaseUri;
            request.Resource = UserInfoServiceEndpoint.Resource;
            request.Method = Method.GET;

            const string name = "access_token";
            var parameter = request.Parameters.FirstOrDefault(x => x.Name == name);
            if (parameter == null)
            {
                request.AddParameter(name, accessToken);
            }
            else
            {
                parameter.Value = accessToken;
            }
            
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