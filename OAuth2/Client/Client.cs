using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2.Parameters;
using RestSharp;

namespace OAuth2.Client
{
    public abstract class Client
    {
        private readonly IRestClient client;
        private readonly IRestRequest request;
        private readonly IConfiguration configuration;

        protected abstract Endpoint AccessCodeServiceEndpoint { get; }
        protected abstract Endpoint AccessTokenServiceEndpoint { get; }
        protected abstract Endpoint UserInfoServiceEndpoint { get; }

        protected Client(IRestClient client, IRestRequest request, IConfiguration configuration)
        {
            this.client = client;
            this.request = request;
            this.configuration = configuration;
        }

        public string GetAccessCodeRequestUri()
        {
            return "{0}?{1}".Fill(
                AccessCodeServiceEndpoint.Uri,
                configuration.Get<AccessCodeRequestParameters>().ToQuerySrting());
        }

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
            var response = client.Execute(request).Content;

            try
            {
                return (string)JObject.Parse(response).SelectToken("access_token");
            }
            catch (JsonReaderException)
            {
                return response.ToDictionary()["access_token"];
            }
        }

        public UserInfo GetUserInfo(string accessToken)
        {
            client.BaseUrl = UserInfoServiceEndpoint.BaseUri;
            request.Resource = UserInfoServiceEndpoint.Resource;
            request.Method = Method.GET;

            request.AddParameter("access_token", accessToken);
            request.AddParameter("fields", "id,first_name,last_name,email,picture");

            return ParseUserInfo(client.Execute(request).Content);
        }

        protected abstract UserInfo ParseUserInfo(string content);
    }
}