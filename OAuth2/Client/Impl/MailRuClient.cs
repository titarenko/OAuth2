using System.Linq;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Mail.Ru authentication client.
    /// </summary>
    public class MailRuClient : OAuth2Client
    {
        private readonly IClientConfiguration configuration;
        /// <summary>
        /// Initializes a new instance of the <see cref="MailRuClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public MailRuClient(IRequestFactory factory, IClientConfiguration configuration) 
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
                    BaseUri = "https://connect.mail.ru",
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
                    BaseUri = "https://connect.mail.ru",
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
                    BaseUri = "http://www.appsmail.ru",
                    Resource = "/platform/api"                    
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(IRestRequest request)
        {
            // Source documents
            // http://api.mail.ru/docs/guides/restapi/
            // http://api.mail.ru/docs/reference/rest/users.getInfo/

            request.AddParameter("app_id", configuration.ClientId);
            request.AddParameter("method", "users.getInfo");
            request.AddParameter("secure", "1");            
            request.AddParameter("session_key", AccessToken);

            // workaround for current design, oauth_token is always present in URL, so we need emulate it for correct request signing 
            var fakeParam = new Parameter() { Name = "oauth_token", Value = AccessToken };
            request.AddParameter(fakeParam);

            //sign=hex_md5('app_id={client_id}method=users.getInfosecure=1session_key={access_token}{secret_key}')
            string signature = string.Concat(request.Parameters.OrderBy(x => x.Name).Select(x => string.Format("{0}={1}", x.Name, x.Value)).ToList());            
            signature = (signature+configuration.ClientSecret).GetMd5Hash();
            
            request.Parameters.Remove(fakeParam);

            request.AddParameter("sig", signature);            
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JArray.Parse(content);
            return new UserInfo
            {
                Id = response[0]["uid"].Value<string>(),
                FirstName = response[0]["first_name"].Value<string>(),
                LastName = response[0]["last_name"].Value<string>(),
                Email = response[0]["email"].Value<string>(),
                PhotoUri = response[0]["pic"].Value<string>()
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string ProviderName
        {
            get { return "MailRu"; }
        }
    }
}