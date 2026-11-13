using System;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Extensions;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Yahoo authentication client.
    /// </summary>
    /// <seealso href="https://developer.yahoo.com/oauth2/guide/">Yahoo OAuth 2.0 Documentation</seealso>
    public class YahooClient : OAuth2Client
    {
        private string? _userProfileGUID;

        /// <summary>
        /// Gets the Yahoo user GUID from the token response or the UserInfo <c>sub</c> claim.
        /// </summary>
        public string? UserProfileGUID
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
        /// Retains the legacy GUID when supplied so it can be checked against the UserInfo subject.
        /// </summary>
        /// <param name="args"></param>
        protected override void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
            using var doc = JsonDocument.Parse(args.Response.Content!); // Non-null: set from verified response
            this._userProfileGUID = doc.RootElement.SelectToken("xoauth_yahoo_guid")?.GetStringValue();
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken!, BearerTokenType);
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
                    BaseUri = "https://api.login.yahoo.com",
                    Resource = "/openid/v1/userinfo"
                };
            }
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;
            if (response.TryGetProperty("sub", out _) || !response.TryGetProperty("profile", out var profile) ||
                profile.ValueKind != JsonValueKind.Object)
            {
                if (!response.TryGetProperty("sub", out var subject) || subject.ValueKind != JsonValueKind.String ||
                    String.IsNullOrWhiteSpace(subject.GetString()))
                    throw new UnexpectedResponseException("sub");

                var id = subject.GetString();
                if (!String.IsNullOrEmpty(_userProfileGUID) && !String.Equals(_userProfileGUID, id, StringComparison.Ordinal))
                    throw new UnexpectedResponseException("sub");

                UserProfileGUID = id;
                var picture = response.GetStringOrDefault("picture");
                return new UserInfo
                {
                    Id = id,
                    ProviderName = Name,
                    FirstName = response.GetStringOrDefault("given_name"),
                    LastName = response.GetStringOrDefault("family_name"),
                    Email = response.GetStringOrDefault("email"),
                    EmailVerified = response.GetBooleanOrDefault("email_verified"),
                    AvatarUri =
                    {
                        Small = response.SelectToken("profile_images.image32")?.GetStringValue() ?? picture,
                        Normal = response.SelectToken("profile_images.image64")?.GetStringValue() ?? picture,
                        Large = response.SelectToken("profile_images.image128")?.GetStringValue() ?? picture
                    }
                };
            }

            var userInfo = new UserInfo();
            userInfo.AvatarUri.Normal =
                userInfo.AvatarUri.Large =
                userInfo.AvatarUri.Small = response.SelectToken("profile.image.imageUrl")?.GetStringValue();

            userInfo.FirstName = response.SelectToken("profile.givenName")?.GetStringValue();
            userInfo.LastName = response.SelectToken("profile.familyName")?.GetStringValue();
            userInfo.Id = this._userProfileGUID;
            userInfo.Email = response.SelectToken("emails")?.GetStringValue();
            userInfo.ProviderName = this.Name;
            return userInfo;
        }
    }
}
