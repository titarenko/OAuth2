using System;
using System.Linq;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Extensions;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Mail.Ru authentication client.
    /// </summary>
    /// <seealso href="https://api.mail.ru/docs/guides/oauth/">Mail.Ru OAuth Documentation</seealso>
    public class MailRuClient : OAuth2Client
    {
        private readonly IClientConfiguration _configuration;
        /// <summary>
        /// Initializes a new instance of the <see cref="MailRuClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public MailRuClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://www.appsmail.ru",
                    Resource = "/platform/api"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            // Source documents
            // http://api.mail.ru/docs/guides/restapi/
            // http://api.mail.ru/docs/reference/rest/users.getInfo/

            args.Request.AddParameter("app_id", _configuration.ClientId);
            args.Request.AddParameter("method", "users.getInfo");
            args.Request.AddParameter("secure", "1");
            args.Request.AddParameter("session_key", AccessToken);

            // workaround for current design, oauth_token is always present in URL, so we need emulate it for correct request signing
            var fakeParam = new QueryParameter("oauth_token", AccessToken);
            args.Request.AddParameter(fakeParam);

            //sign=hex_md5('app_id={client_id}method=users.getInfosecure=1session_key={access_token}{secret_key}')
            string signature = String.Concat(args.Request.Parameters.OrderBy(x => x.Name).Select(x => String.Format("{0}={1}", x.Name, x.Value)).ToList());
            signature = (signature+_configuration.ClientSecret).GetMd5Hash();

            args.Request.Parameters.RemoveParameter(fakeParam);

            args.Request.AddParameter("sig", signature);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var first = doc.RootElement[0];
            var avatarUri = first.GetProperty("pic").GetString();
            return new UserInfo
            {
                Id = first.GetProperty("uid").GetStringValue(),
                FirstName = first.GetProperty("first_name").GetString(),
                LastName = first.GetProperty("last_name").GetString(),
                Email = first.GetStringOrDefault("email"),
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
            get { return "MailRu"; }
        }
    }
}
