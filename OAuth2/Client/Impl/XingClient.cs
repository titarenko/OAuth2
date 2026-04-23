using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Xing authentication client.
    /// </summary>
    /// <remarks>
    /// <para>The Xing REST API (<c>api.xing.com/v1/</c>) used by this client has been discontinued.
    /// While <c>dev.xing.com</c> still hosts "Login with XING" and "Share on XING" plugins,
    /// the OAuth 1.0a REST API for user profile data is no longer available. Xing was
    /// rebranded under New Work SE.</para>
    /// </remarks>
    /// <seealso href="https://dev.xing.com/">Xing Developer Portal (plugins only)</seealso>
    public class XingClient : OAuthClient
    {
        private const string BaseApiUrl = "https://api.xing.com";

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public XingClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        /// <inheritdoc cref="OAuthClient.Name" />
        public override string Name
        {
            get { return "Xing"; }
        }

        /// <inheritdoc cref="OAuthClient.AccessTokenServiceEndpoint" />
        protected override Endpoint RequestTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = BaseApiUrl,
                    Resource = "/v1/request_token"
                };
            }
        }

        /// <inheritdoc cref="OAuthClient.AccessTokenServiceEndpoint" />
        protected override Endpoint LoginServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = BaseApiUrl,
                    Resource = "/v1/authorize"
                };
            }
        }

        /// <inheritdoc cref="OAuthClient.AccessTokenServiceEndpoint" />
        protected override Endpoint AccessTokenServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = BaseApiUrl,
                    Resource = "/v1/access_token"
                };
            }
        }

        /// <inheritdoc cref="OAuthClient.UserInfoServiceEndpoint" />
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                return new Endpoint
                {
                    BaseUri = BaseApiUrl,
                    Resource = "/v1/users/me"
                };
            }
        }

        /// <inheritdoc cref="OAuthClient.ParseUserInfo" />
        protected override UserInfo ParseUserInfo(string content)
        {
            var users = JsonSerializer.Deserialize<UserContainer>(content);
            var userInfo = new UserInfo();

            if (users != null && users.Users != null && users.Users.Count > 0)
            {
                userInfo.Id = users.Users[0].Id;
                userInfo.FirstName = users.Users[0].FirstName;
                userInfo.LastName = users.Users[0].LastName;
                userInfo.Email = users.Users[0].Email;
                if (users.Users[0].PhotoUrls != null)
                {
                    userInfo.AvatarUri.Small = users.Users[0].PhotoUrls.Small;
                    userInfo.AvatarUri.Normal = users.Users[0].PhotoUrls.Normal;
                    userInfo.AvatarUri.Large = users.Users[0].PhotoUrls.Large;
                }
            }

            return userInfo;
        }

        private class UserContainer
        {
            [JsonPropertyName("users")]
            public List<User> Users { get; set; }
        }

        private class User
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("first_name")]
            public string FirstName { get; set; }

            [JsonPropertyName("last_name")]
            public string LastName { get; set; }

            [JsonPropertyName("active_email")]
            public string Email { get; set; }

            [JsonPropertyName("photo_urls")]
            public PhotoUrls PhotoUrls { get; set; }
        }

        private class PhotoUrls
        {
            [JsonPropertyName("size_48x48")]
            public string Small { get; set; }

            [JsonPropertyName("size_128x128")]
            public string Normal { get; set; }

            [JsonPropertyName("size_256x256")]
            public string Large { get; set; }
        }
    }
}
