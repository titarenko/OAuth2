using System;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Zuul authentication client.
    /// </summary>
    public class ZuulClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZuulClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public ZuulClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
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
					BaseUri = "http://zuul.is-valid.org/",
                    Resource = "/oauth/authorize"
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
					BaseUri = "http://zuul.is-valid.org/",
                    Resource = "/oauth/token"
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
					BaseUri = "http://zuul.is-valid.org",
					Resource = "/oauth/user.json"                    
				};
			}
		}

         /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Zuul"; }
        }


		protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
		{
			args.Client.Authenticator = null;
			args.Request.Parameters.Add(new Parameter
				{
					Name  = "access_token",
					Type  = ParameterType.GetOrPost,
					Value = AccessToken
				});
		}                        

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);

            return new UserInfo
            {
                Id = response["id"].Value<string>(),
                Email = response["email"].SafeGet(x => x.Value<string>()),
            };
        }
    }
}
