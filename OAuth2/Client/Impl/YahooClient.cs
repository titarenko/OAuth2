using System;

using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Yahoo client 
    /// Right now only Yahoo Gemini and Yahoo Social support OAuth2
    /// https://developer.yahoo.com/oauth2/guide/
    /// </summary>
    public class YahooClient : OAuth2Client
    {
        private string _userProfileGUID;

        public string UserProfileGUID
        {
            get
            {
                return _userProfileGUID;
            }
            private set
            {
                _userProfileGUID = value;
            }
        }        

        /// <summary>
        /// Initializes a new instance of the <see cref="YahooClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public YahooClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Yahoo client name
        /// </summary>
        public override string Name
        {
            get
            {
                return "Yahoo";
            }
        }

        /// <summary>
        /// The access code service endpoint
        /// </summary>
        protected override Endpoint AccessCodeServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.login.yahoo.com",
                    Resource = "/oauth2/request_auth"
                };
            }
        }

        /// <summary>
        /// The acess token service endpoint
        /// </summary>
        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = "https://api.login.yahoo.com",
                    Resource = "/oauth2/get_token"
                };
            }
        }

        /// <summary>
        /// It's required to store the User GUID obtained in the response for further usage
        /// https://developer.yahoo.com/oauth2/guide/flows_authcode/
        /// </summary>
        /// <param name="args"></param>
        protected override void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
            var responseJObject = JObject.Parse(args.Response.Content);
            this._userProfileGUID = responseJObject.SelectToken("xoauth_yahoo_guid")?.ToString();
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// 
        /// We have to reformat the Url adding the user guid for accessing their information
        /// https://developer.yahoo.com/oauth2/guide/apirequests/
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "Bearer");
            args.Request.Resource = string.Format(this.UserInfoServiceEndpoint.Resource, this._userProfileGUID);
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
                    BaseUri = "https://social.yahooapis.com",
                    Resource = "v1/user/{0}/profile?format=json"
                };
            }
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var response = JObject.Parse(content);
            var userInfo = new UserInfo();
            userInfo.AvatarUri.Normal =
                userInfo.AvatarUri.Large =
                userInfo.AvatarUri.Small = response.SelectToken("profile.image.imageUrl")?.ToString();

            userInfo.FirstName = response.SelectToken("profile.givenName")?.ToString();
            userInfo.LastName = response.SelectToken("profile.familyName")?.ToString();
            userInfo.Id = this._userProfileGUID;
            userInfo.Email = response.SelectToken("emails")?.ToString();
            userInfo.ProviderName = this.Name;
            return userInfo;
        }
    }
}
