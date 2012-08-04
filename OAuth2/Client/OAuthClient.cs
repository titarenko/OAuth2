using System;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client
{
    public abstract class OAuthClient
    {
        private readonly IRestClient client;
        private readonly IRestRequest request;

        protected Endpoint RequestTokenRequestEndpoint { get; set; }
        protected Endpoint LoginServiceEndpoint { get; set; }
        protected Endpoint AccessTokenRequestEndpoint { get; set; }
        //protected Endpoint 

        protected OAuthClient(IRestClient client, IRestRequest request, IConfiguration configuration)
        {
            this.client = client;
            this.request = request;
        }

        public string GetLoginRequestUri()
        {
            client.BaseUrl = RequestTokenRequestEndpoint.BaseUri;
            request.Resource = RequestTokenRequestEndpoint.Resource;
            request.Method = Method.POST;

            var response = client.Execute(request);

            return "{0}?{1}".Fill(LoginServiceEndpoint.Uri, response.Content.ToDictionary()["access_token"]);
        }

        public UserInfo GetUserInfo(string response)
        {
            client.BaseUrl = AccessTokenRequestEndpoint.BaseUri;
            request.Resource = AccessTokenRequestEndpoint.Resource;
            request.Method = Method.POST;

            var restResponse = client.Execute(request);

            throw new NotImplementedException();
        }
    }
}