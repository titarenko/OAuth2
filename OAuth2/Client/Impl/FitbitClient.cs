using System.Linq;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Instagram authentication client.
    /// </summary>
    public class FitbitClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FitbitClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public FitbitClient(IRequestFactory factory, IClientConfiguration configuration) 
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
                    BaseUri = "https://www.fitbit.com",
                    Resource = "/oauth2/authorize"
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
                    BaseUri = "https://api.fitbit.com",
                    Resource = "/oauth2/token"
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
                    BaseUri = "https://api.fitbit.com",
                    Resource = "/1/user/-/profile.json"
                };
            }
        }

        /// <summary>
        /// Adds Fitbit's required Authorization header in format base64Encode(client_id:client secret)
        /// See here: https://dev.fitbit.com/docs/oauth2/#authorization-header
        /// </summary>
        /// <param name="args"></param>
        private void AddAuthorizationHeader(BeforeAfterRequestArgs args)
        {
            args.Request.Parameters.Add(new RestSharp.Parameter
            {
                Type = RestSharp.ParameterType.HttpHeader,
                Name = "Authorization",
                Value = "Basic " +
                    System.Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Configuration.ClientId, Configuration.ClientSecret)))

            });
        }

        protected override void BeforeGetAccessToken(BeforeAfterRequestArgs args)
        {
            this.AddAuthorizationHeader(args);
            base.BeforeGetAccessToken(args);
        }

        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "Bearer");
            base.BeforeGetUserInfo(args);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            var names = response["user"]["fullName"].Value<string>().Split(' ');
            var avatarUri = response["user"]["avatar"].Value<string>();
            return new UserInfo
            {
                Id = response["user"]["encodedId"].Value<string>(),
                FirstName = names.Any() ? names.First() : response["user"]["displayName"].Value<string>(),
                LastName = names.Count() > 1 ? names.Last() : string.Empty,
                AvatarUri =
                    {
                        Small = null,
                        Normal = avatarUri,
                        Large = null
                    }
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Fitbit"; }
        }
    }
}