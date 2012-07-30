﻿using Newtonsoft.Json.Linq;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client
{
    /// <summary>
    /// VK (Vkontakte) authentication client.
    /// </summary>
    public class VkClient : Client
    {
        private string userId;

        /// <summary>
        /// Initializes a new instance of the <see cref="VkClient"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="request">The request.</param>
        /// <param name="configuration">The configuration.</param>
        public VkClient(IRestClient client, IRestRequest request, IConfiguration configuration)
            : base(client, request, configuration)
        {
        }
        
        /// <summary>
        /// Defines URI of service which issues access code.
        /// </summary>
        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "http://oauth.vk.com",
                    Resource = "/authorize"
                };
            }
        }

        /// <summary>
        /// Defines URI of service which issues access token.
        /// </summary>
        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://oauth.vk.com",
                    Resource = "/access_token"
                };
            }
        }

        /// <summary>
        /// Defines URI of service which allows to obtain information about user which is currently logged in.
        /// </summary>
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.vk.com",
                    Resource = "/method/users.get"
                };
            }
        }

        /// <summary>
        /// Called just after obtaining response with access token from third-party service.
        /// Allows to read extra data returned along with access token.
        /// </summary>
        /// <param name="response"></param>
        protected override void AfterGetAccessToken(IRestResponse response)
        {
            userId = JObject.Parse(response.Content)["user_id"].Value<string>();
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        /// <param name="request"></param>
        protected override void OnGetUserInfo(IRestRequest request)
        {
            request.AddParameter("uids", userId);
            request.AddParameter("fields", "uid,first_name,last_name,photo");
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        /// <returns></returns>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content)["response"][0];
            return new UserInfo
            {
                Email = null,
                FirstName = response["first_name"].Value<string>(),
                LastName = response["last_name"].Value<string>(),
                Id = response["uid"].Value<string>(),
                PhotoUri = response["photo"].Value<string>()
            };
        }
    }
}