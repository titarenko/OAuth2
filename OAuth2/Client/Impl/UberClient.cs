using System;

using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Uber authentication client
    /// </summary>
    public class UberClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UberClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public UberClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        public override string Name
        {
            get
            {
                return "Uber";
            }
        }

        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            { 
                return new Endpoint
                {
                    BaseUri = "https://login.uber.com",
                    Resource = "/oauth/v2/authorize"
                };
            }
        }

        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://login.uber.com",
                    Resource = "/oauth/v2/token"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "Bearer");
        }
        
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.uber.com",
                    Resource = "/v1/me"
                };
            }
        }

        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            var userInfo = new UserInfo();
            JToken firstName, lastName, email, picture;
            if (response.TryGetValue("first_name", StringComparison.OrdinalIgnoreCase, out firstName))
            {
                userInfo.FirstName = firstName.ToString();
            }

            if (response.TryGetValue("last_name", StringComparison.OrdinalIgnoreCase, out lastName))
            {
                userInfo.LastName = lastName.ToString();
            }

            if (response.TryGetValue("email", StringComparison.OrdinalIgnoreCase, out email))
            {
                userInfo.Email = email.ToString();
            }

            if (response.TryGetValue("picture", StringComparison.OrdinalIgnoreCase, out picture))
            {
                var pictureUri = picture.ToString();
                userInfo.AvatarUri.Small = pictureUri;
                userInfo.AvatarUri.Normal = pictureUri;
                userInfo.AvatarUri.Large = pictureUri;
            }

            return userInfo;
         }
    }
}
