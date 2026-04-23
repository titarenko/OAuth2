using System;
using System.Linq;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Extensions;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Yandex authentication client.
    /// </summary>
    public class YandexClient : OAuth2Client
    {
		private static readonly string _avatarBaseUri 	= "https://avatars.yandex.net/get-yapic/";
		private static readonly string _small			= "islands-middle";
		private static readonly string _normal			= "islands-retina-50";
		private static readonly string _large			= "islands-200";

		/// <summary>
		/// Initializes a new instance of the <see cref="YandexClient"/> class.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="configuration">The configuration.</param>
		public YandexClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://oauth.yandex.ru",
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
                    BaseUri = "https://oauth.yandex.ru",
                    Resource = "/token"
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
                    BaseUri = "https://login.yandex.ru",
                    Resource = "/info"
                };
            }
        }

        /// <summary>
        /// Called just before issuing request to third-party service when everything is ready.
        /// Allows to add extra parameters to request or do any other needed preparations.
        /// </summary>
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            // Source document
            // http://api.yandex.com/oauth/doc/dg/yandex-oauth-dg.pdf
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;
            var names = response.GetProperty("real_name").GetString().Split(' ');
			var avatar = response.GetProperty("default_avatar_id").GetString();

			var user = new UserInfo
            {
                Id = response.GetProperty("id").GetStringValue(),
                FirstName = names.Any() ? names.First() : response.GetProperty("display_name").GetString(),
                LastName = names.Count() > 1 ? names.Last() : String.Empty,
                Email = response.GetStringOrDefault("default_email"),
            };

			if (!String.IsNullOrEmpty(avatar))
			{
				avatar = _avatarBaseUri + avatar + "/";
				user.AvatarUri.Small = avatar+_small;
				user.AvatarUri.Normal = avatar+_normal;
				user.AvatarUri.Large = avatar+_large;
			}

			return user;
		}

        /// <inheritdoc />
        public override string Name
        {
            get { return "Yandex"; }
        }
    }
}