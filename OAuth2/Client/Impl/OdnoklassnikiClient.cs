using System.Linq;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Odnoklassniki authentication client.
    /// </summary>
    public class OdnoklassnikiClient : OAuth2Client
    {
        private readonly IClientConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="OdnoklassnikiClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public OdnoklassnikiClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
            _configuration = configuration;
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
                    BaseUri = "http://www.odnoklassniki.ru",
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
                    BaseUri = "http://api.odnoklassniki.ru",
                    Resource = "/oauth/token.do"
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
                    BaseUri = "http://api.odnoklassniki.ru",
                    Resource = "/fb.do"
                };
            }
        }
        
        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            // Source document
            // http://dev.odnoklassniki.ru/wiki/pages/viewpage.action?pageId=12878032

            args.Request.AddParameter("application_key", _configuration.ClientPublic);
            args.Request.AddParameter("method", "users.getCurrentUser");

            // workaround for current design, oauth_token is always present in URL, so we need emulate it for correct request signing 
            var fakeParam = new Parameter() { Name = "oauth_token", Value = AccessToken };
            args.Request.AddParameter(fakeParam);

            // Signing.
            // Call API methods using access_token instead of session_key parameter
            // Calculate every request signature parameter sig using a little bit different way described in
            // http://dev.odnoklassniki.ru/wiki/display/ok/Authentication+and+Authorization
            // sig = md5( request_params_composed_string+ md5(access_token + application_secret_key)  )
            // Don't include access_token into request_params_composed_string
            string signature = string.Concat(args.Request.Parameters.OrderBy(x => x.Name).Select(x => string.Format("{0}={1}", x.Name, x.Value)).ToList());
            signature = (signature + (AccessToken + _configuration.ClientSecret).GetMd5Hash()).GetMd5Hash();

            // Removing fake param to prevent dups
            args.Request.Parameters.Remove(fakeParam);

            args.Request.AddParameter("access_token", AccessToken);
            args.Request.AddParameter("sig", signature);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            var avatarUri = response["pic_1"].Value<string>();
            return new UserInfo
            {
                Id = response["uid"].Value<string>(),
                FirstName = response["first_name"].Value<string>(),
                LastName = response["last_name"].Value<string>(),                
                AvatarUri =
                    {
                        Small = null,
                        Normal = avatarUri,
                        Large = avatarUri.Replace("&photoType=4", "&photoType=6")
                    }                
            };
        }
        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Odnoklassniki"; }
        }
    }
}