using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// GitHub authentication client.
    /// </summary>
    public class GitHubClient : OAuth2Client
    {
        private readonly IRequestFactory _factory;

        public GitHubClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
            _factory = factory;
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
            var names = (cnt["name"].SafeGet(x => x.Value<string>()) ?? string.Empty).Split(new []{ " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
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

        protected override async Task<UserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            var userInfo = await base.GetUserInfoAsync(cancellationToken).ConfigureAwait(false);
            if (userInfo == null)
                return null;

            if (!String.IsNullOrEmpty(userInfo.Email))
                return userInfo;

            var client = _factory.CreateClient(UserEmailServiceEndpoint);
            client.Authenticator = new OAuth2UriQueryParameterAuthenticator(AccessToken);
            var request = _factory.CreateRequest(UserEmailServiceEndpoint);

            BeforeGetUserInfo(new BeforeAfterRequestArgs
            {
                Client = client,
                Request = request,
                Configuration = Configuration
            });

            var response = await client.ExecuteAndVerifyAsync(request, cancellationToken).ConfigureAwait(false);
            var userEmails = ParseEmailAddresses(response.Content).Where(u => !String.IsNullOrEmpty(u.Email)).ToList();

            string primaryEmail = userEmails.Where(u => u.Primary).Select(u => u.Email).FirstOrDefault();
            string verifiedEmail = userEmails.Where(u => u.Verified).Select(u => u.Email).FirstOrDefault();
            string fallbackEmail = userEmails.Select(u => u.Email).FirstOrDefault();
            userInfo.Email = primaryEmail ?? verifiedEmail ?? fallbackEmail;

            return userInfo;
        }

        protected virtual List<UserEmails> ParseEmailAddresses(string content)
        {
            return JsonConvert.DeserializeObject<List<UserEmails>>(content);
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
            get { return new Endpoint { BaseUri = "https://api.github.com", Resource = "/user" }; }
        }

        protected virtual Endpoint UserEmailServiceEndpoint
        {
            get { return new Endpoint { BaseUri = "https://api.github.com", Resource = "/user/emails" }; }
        }

        protected class UserEmails
        {
            public string Email { get; set; }
            public bool Primary { get; set; }
            public bool Verified { get; set; }
        }
    }
}
