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
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// GitHub authentication client.
    /// </summary>
    public class GitHubClient : OAuth2Client
    {
        private readonly IRequestFactory _factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubClient"/> class.
        /// </summary>
        /// <param name="factory">The factory used to create HTTP requests.</param>
        /// <param name="configuration">The client configuration.</param>
        public GitHubClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
            _factory = factory;
        }

        /// <inheritdoc />
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
            var names = (cnt["name"].SafeGet(x => x.Value<string>()) ?? String.Empty).Split(new []{ " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            const string avatarUriTemplate = "{0}&s={1}";
            var avatarUri = cnt["avatar_url"].Value<string>();
            var result = new UserInfo
                {
                    Email = cnt["email"].SafeGet(x => x.Value<string>()),
                    ProviderName = this.Name,
                    Id = cnt["id"].Value<string>(),
                    FirstName = names.Count > 0 ? names.First() : cnt["login"].Value<string>(),
                    LastName = names.Count > 1 ? names.Last() : String.Empty,
                    AvatarUri =
                        {
                            Small = !String.IsNullOrWhiteSpace(avatarUri) ? String.Format(avatarUriTemplate, avatarUri, AvatarInfo.SmallSize) : String.Empty,
                            Normal = avatarUri,
                            Large = !String.IsNullOrWhiteSpace(avatarUri) ? String.Format(avatarUriTemplate, avatarUri, AvatarInfo.LargeSize) : String.Empty
                        }
                };

            return result;
        }

        /// <inheritdoc />
        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "token");
        }

        /// <inheritdoc />
        protected override async Task<UserInfo> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            var userInfo = await base.GetUserInfoAsync(cancellationToken).ConfigureAwait(false);
            if (userInfo == null)
                return null;

            if (!String.IsNullOrEmpty(userInfo.Email))
                return userInfo;

            var client = _factory.CreateClient(UserEmailServiceEndpoint);
            var request = _factory.CreateRequest(UserEmailServiceEndpoint);
            request.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "token");

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

        /// <summary>
        /// Parses the email addresses from the GitHub user emails API response.
        /// </summary>
        /// <param name="content">The JSON content returned from the user emails endpoint.</param>
        /// <returns>A list of <see cref="UserEmails"/> representing the user's email addresses.</returns>
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

        /// <summary>
        /// Defines URI of service which allows to obtain email addresses of user which is currently logged in.
        /// </summary>
        protected virtual Endpoint UserEmailServiceEndpoint
        {
            get { return new Endpoint { BaseUri = "https://api.github.com", Resource = "/user/emails" }; }
        }

        /// <summary>
        /// Represents an email address returned by the GitHub user emails API.
        /// </summary>
        protected class UserEmails
        {
            /// <summary>
            /// Gets or sets the email address.
            /// </summary>
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this is the user's primary email.
            /// </summary>
            public bool Primary { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the email address has been verified.
            /// </summary>
            public bool Verified { get; set; }
        }
    }
}
