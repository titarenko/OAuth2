using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// GitHub authentication client.
    /// </summary>
    public class GitHubClient : OAuth2Client
    {
        public GitHubClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }

        protected override void BeforeGetAccessToken(BeforeAfterRequestArgs args)
        {
            args.Request.AddObject(new
            {
                code = args.Parameters.GetOrThrowUnexpectedResponse("code"),
                client_id = args.Configuration.ClientId,
                client_secret = args.Configuration.ClientSecret,
                redirect_uri = args.Configuration.RedirectUri,
                state = State,
            });
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var cnt = JObject.Parse(content);
            var names = cnt["name"].Value<string>().Split(' ').ToList();
            const string avatarUriTemplate = "{0}&s={1}";
            var avatarUri = cnt["avatar_url"].Value<string>();
            var result = new UserInfo
                {
                    Email = cnt["email"].SafeGet(x => x.Value<string>()),
                    ProviderName = this.Name,
                    Id = cnt["id"].Value<string>(),
                    FirstName = names.Count > 0 ? names.First() : cnt["login"].Value<string>(),
                    LastName = names.Count > 1 ? names.Last() : string.Empty,
                    AvatarUri =
                        {
                            Small = !string.IsNullOrWhiteSpace(avatarUri) ? string.Format(avatarUriTemplate, avatarUri, AvatarInfo.SmallSize) : string.Empty,
                            Normal = avatarUri,
                            Large = !string.IsNullOrWhiteSpace(avatarUri) ? string.Format(avatarUriTemplate, avatarUri, AvatarInfo.LargeSize) : string.Empty
                        }
                };
            return result;
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "GitHub"; }
        }

        /// <summary>
        /// Defines URI of service which issues access code.
        /// </summary>
        protected override Endpoint AccessCodeServiceEndpoint
        {
            get { return new Endpoint { BaseUri = "https://github.com", Resource = "/login/oauth/authorize" }; }
        }

        /// <summary>
        /// Defines URI of service which issues access token.
        /// </summary>
        protected override Endpoint AccessTokenServiceEndpoint
        {
            get { return new Endpoint { BaseUri = "https://github.com", Resource = "/login/oauth/access_token" }; }
        }

        /// <summary>
        /// Defines URI of service which allows to obtain information about user which is currently logged in.
        /// </summary>
        protected override Endpoint UserInfoServiceEndpoint
        {
            get { return new Endpoint { BaseUri = "https://api.github.com/", Resource = "/user" }; }
        }
    }
}
