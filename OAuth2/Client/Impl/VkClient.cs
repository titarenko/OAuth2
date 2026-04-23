using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Extensions;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// VK (Vkontakte) authentication client.
    /// </summary>
    /// <remarks>
    /// <para>Updated to VK API v5.131 (previous v5.74 was deprecated). See issue #146.</para>
    /// </remarks>
    /// <seealso href="https://dev.vk.com/en/api/access-token/authcode-flow-user">VK OAuth Documentation</seealso>
    public class VkClient : OAuth2Client
    {
        private string _userId;
        private string _email;

        /// <summary>
        /// Initializes a new instance of the <see cref="VkClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public VkClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://oauth.vk.com",
                    Resource = "/authorize"
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
                    BaseUri = "https://oauth.vk.com",
                    Resource = "/access_token"
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
                    BaseUri = "https://api.vk.com",
                    Resource = "/method/users.get"
                };
            }
        }

        /// <inheritdoc />
        public override string Name
        {
            get { return "Vkontakte"; }
        }

        /// <summary>
        /// Called just after obtaining response with access token from third-party service.
        /// Allows to read extra data returned along with access token.
        /// </summary>
        protected override void AfterGetAccessToken(BeforeAfterRequestArgs args)
        {
            using var doc = JsonDocument.Parse(args.Response.Content);
            var instance = doc.RootElement;
            _userId = instance.GetProperty("user_id").GetStringValue();
            if (instance.TryGetProperty("email", out var email) && email.ValueKind != JsonValueKind.Null)
                _email = email.GetString();
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.AddParameter("v", "5.131");
            args.Request.AddParameter("user_ids", _userId);
            args.Request.AddParameter("fields", "first_name,last_name,has_photo,photo_max_orig");
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement.GetProperty("response")[0];
            var hasPhotoElement = response.GetProperty("has_photo");
            var hasPhoto = hasPhotoElement.ValueKind == JsonValueKind.True
                || (hasPhotoElement.ValueKind == JsonValueKind.Number && hasPhotoElement.GetInt32() != 0);
            var avatarUri = hasPhoto ? response.GetProperty("photo_max_orig").GetString() : null;
            return new UserInfo
            {
                Email = _email,
                FirstName = response.GetProperty("first_name").GetString(),
                LastName = response.GetProperty("last_name").GetString(),
                Id = response.GetProperty("id").GetStringValue(),
                AvatarUri =
                {
                    Small = null,
                    Normal = avatarUri,
                    Large = null
                }
            };
        }
    }
}
