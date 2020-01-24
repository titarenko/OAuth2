using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace ServRS.External
{
    public class LoginCidadaoClient : OAuth2Client
    {
        private readonly IRequestFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginCidadaoClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public LoginCidadaoClient(IRequestFactory factory, 
            IClientConfiguration configuration)
            : base(factory, configuration)
        {
            _factory = factory;
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
                    BaseUri = "https://logincidadao.rs.gov.br",
                    Resource = "/openid/connect/authorize"
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
                    BaseUri = "https://logincidadao.rs.gov.br",
                    Resource = "/openid/connect/token"
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
                    BaseUri = "https://logincidadao.rs.gov.br",
                    Resource = "/api/v2/person"
                };
            }
        }

        /// <summary>
        /// Obtains user information using provider API.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellationtoken</param>
        protected override async Task<OAuth2.Models.UserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            var client = _factory.CreateClient(UserInfoServiceEndpoint);
            var request = _factory.CreateRequest(UserInfoServiceEndpoint);

            BeforeGetUserInfo(new BeforeAfterRequestArgs
            {
                Client = client,
                Request = request,
                Configuration = Configuration
            });
            var response = await client.ExecuteAndVerifyAsync(request, cancellationToken).ConfigureAwait(false);

            var result = ParseUserInfo(response.Content);
            result.ProviderName = Name;

            return result;
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.AddHeader("Authorization", "Bearer " + AccessToken);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            return new Cidadao
            {
                FirstName = response["first_name"].Value<string>(),
                LastName = response["last_name"].Value<string>(),
                Cpf = response["cpf"].Value<string>(),
                Email = response["email"].SafeGet(x => x.Value<string>()),
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Login Cidadão"; }
        }

        public class Cidadao : UserInfo
        {
            public string Cpf { get; set; }
        }

    }

}

