using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OAuth2.Client
{
    /// <summary>
    /// Facebook authentication client.
    /// </summary>
    public class OdnoklassnikiClient : OAuth2Client
    {
        private readonly IClientConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public OdnoklassnikiClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
            this.configuration = configuration;
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
        /// Returns MD5 Hash of input.
        /// </summary>
        /// <param name="line">The line.</param>
        public static string GetMD5(string input)
        {
            var x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            var bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            var s = new StringBuilder();
            foreach (var b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            return s.ToString();
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(IRestRequest request)
        {
            // Source document
            // http://dev.odnoklassniki.ru/wiki/pages/viewpage.action?pageId=12878032

            request.AddParameter("application_key", configuration.ClientPublicKey);
            request.AddParameter("method", "users.getCurrentUser");

            // workaround for current design, oauth_token is always present in URL, so we need emulate it for correct request signing 
            var tempParam = new Parameter() { Name = "oauth_token", Value = AccessToken };
            request.AddParameter(tempParam);

            // Signing.
            // Call API methods using access_token instead of session_key parameter
            // Calculate every request signature parameter sig using a little bit different way described in
            // http://dev.odnoklassniki.ru/wiki/display/ok/Authentication+and+Authorization
            // sig = md5( request_params_composed_string+ md5(access_token + application_secret_key)  )
            // Don't include access_token into request_params_composed_string
            string signature = string.Concat(request.Parameters.OrderBy(x => x.Name).Select(x => string.Format("{0}={1}", x.Name, x.Value)).ToList());
            signature = GetMD5(signature + GetMD5(AccessToken + configuration.ClientSecret));

            // Removing temp param to prevent dups
            request.Parameters.Remove(tempParam);

            request.AddParameter("access_token", AccessToken);
            request.AddParameter("sig", signature);
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
                Id = response["uid"].Value<string>(),
                FirstName = response["first_name"].Value<string>(),
                LastName = response["last_name"].Value<string>(),
                PhotoUri = response["pic_1"].Value<string>()
            };
        }

        public override string ProviderName
        {
            get { return "Odnoklassniki"; }
        }
    }
}