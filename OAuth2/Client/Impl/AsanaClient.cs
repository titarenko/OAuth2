using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using RestSharp;
using OAuth2.Infrastructure;
using OAuth2.Models;

using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Asana authentication client.
    /// </summary>
    public class AsanaClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsanaClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public AsanaClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://app.asana.com",
                    Resource = "/-/oauth_authorize"
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
                    BaseUri = "https://app.asana.com",
                    Resource = "/-/oauth_token"
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
                    BaseUri = "https://app.asana.com",
                    Resource = "/api/1.0/users/me"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.AddParameter("opt_fields", "id,name,photo.image_128x128,photo.image_60x60,photo.image_36x36,email");
            args.Request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "Bearer");
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            if (!response.TryGetValue("data", out JToken dataExists))
                return new UserInfo();

            //const string avatarUriTemplate = "{0}?type={1}";
            var avatarSmallUri = response["data"]["photo"]["image_36x36"].Value<string>();
            var avatarNormalUri = response["data"]["photo"]["image_60x60"].Value<string>();
            var avatarLargeUri = response["data"]["photo"]["image_128x128"].Value<string>();
            var splitName = new List<string>(response["data"]["name"].Value<string>().Split(' '));
            var firstName = splitName.FirstOrDefault();
            splitName.RemoveAt(0);
            var lastName = splitName.Join(" ");

            return new UserInfo
            {
                Id = response["data"]["id"].Value<string>(),
                FirstName = firstName,
                LastName = lastName,
                Email = response["data"]["email"].SafeGet(x => x.Value<string>()),
                AvatarUri =
                {
                    Small = !String.IsNullOrWhiteSpace(avatarSmallUri) ? avatarSmallUri : String.Empty,
                    Normal = !String.IsNullOrWhiteSpace(avatarNormalUri) ? avatarNormalUri : String.Empty,
                    Large = !String.IsNullOrWhiteSpace(avatarLargeUri) ? avatarLargeUri : String.Empty
                }
            };
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Asana"; }
        }
    }
}