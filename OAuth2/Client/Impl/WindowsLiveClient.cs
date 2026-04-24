using System;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Extensions;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Windows Live authentication client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This client uses the legacy Windows Live / Live Connect endpoints (login.live.com
    /// and apis.live.net/v5.0). Although Microsoft officially retired the Live SDK in
    /// November 2018, these endpoints continue to function in practice and are actively
    /// used in production (e.g. Exceptionless with <c>wl.emails</c> scope).
    /// </para>
    /// <para>
    /// For new integrations, consider using <see cref="MicrosoftClient"/> instead, which
    /// targets the Microsoft Identity Platform (v2.0) and Microsoft Graph API. Note that
    /// user IDs differ between the two platforms.
    /// </para>
    /// </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/migrating-from-live-sdk">Migrating from Live SDK to Microsoft Graph</seealso>
    public class WindowsLiveClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsLiveClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public WindowsLiveClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsLiveClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="requestOptions">Optional transport-level options such as timeout.</param>
        public WindowsLiveClient(IRequestFactory factory, IClientConfiguration configuration, RequestOptions? requestOptions)
            : base(factory, configuration, requestOptions)
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
                    BaseUri = "https://login.live.com",
                    Resource = "/oauth20_authorize.srf"
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
                    BaseUri = "https://login.live.com",
                    Resource = "/oauth20_token.srf"
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
                    BaseUri = "https://apis.live.net/v5.0",
                    Resource = "/me"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.AddParameter("access_token", AccessToken);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;
            const string avatarUriTemplate = @"https://cid-{0}.users.storage.live.com/users/0x{0}/myprofile/expressionprofile/profilephoto:Win8Static,{1},UserTileStatic/MeControlXXLUserTile?ck=2&ex=24";
            var id = response.GetProperty("id").GetStringValue();
            var userinfo = new UserInfo
            {
                Id = id,
                FirstName = response.GetProperty("first_name").GetString(),
                LastName = response.GetProperty("last_name").GetString(),
                AvatarUri =
                    {
                        Small = String.Format(avatarUriTemplate, id, "UserTileSmall"),
                        Normal = String.Format(avatarUriTemplate, id, "UserTileSmall"),
                        Large = String.Format(avatarUriTemplate, id, "UserTileLarge")
                    }
            };

            if (Configuration.Scope != null && Configuration.Scope.ToUpperInvariant().Contains("WL.EMAILS"))
            {
                userinfo.Email = response.GetProperty("emails").GetStringOrDefault("preferred");
            }

            return userinfo;
        }

        /// <inheritdoc />
        public override string Name
        {
            get { return "WindowsLive"; }
        }
    }
}
