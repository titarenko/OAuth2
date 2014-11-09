﻿using System;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using System.Threading.Tasks;

namespace OAuth2.Client.Impl
{
    public class DigitalOceanClient : OAuth2Client
    {
        private string _accessToken;

        public DigitalOceanClient(IRequestFactory factory, IClientConfiguration configuration) 
            : base(factory, configuration)
        {
        }

        public override string Name
        {
            get { return "DigitalOcean"; }
        }

        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://cloud.digitalocean.com",
                    Resource = "/v1/oauth/authorize"
                };
            }
        }

        protected override void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
             _accessToken = args.Response.GetContent();
        }

        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://cloud.digitalocean.com",
                    Resource = "/v1/oauth/token"
                };
            }
        }

        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override async Task<UserInfo> GetUserInfo()
        {
            return await Task<UserInfo>.Factory.StartNew(() => ParseUserInfo(_accessToken));
        }

        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            return new UserInfo
            {
                Id = response["uid"].Value<string>(),
                FirstName = response["info"]["name"].Value<string>(),
                LastName = "",
                Email = response["info"]["email"].SafeGet(x => x.Value<string>())
            };
        }
    }
}
