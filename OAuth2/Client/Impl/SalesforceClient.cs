using System;
using System.Text.Json;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Salesforce authentication client.
    /// </summary>
    public class SalesforceClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SalesforceClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public SalesforceClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri = "https://login.salesforce.com",
                    Resource = "/services/oauth2/authorize"
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
                    BaseUri = "https://login.salesforce.com",
                    Resource = "/services/oauth2/token"
                };
            }
        }

        /// <summary>
        /// Gets or sets the Salesforce user profile URL returned in the token response.
        /// </summary>
        public string SalesforceProfileUrl { get; set; }

        /// <summary>
        /// Defines URI of service which allows to obtain information about user which is currently logged in.
        /// </summary>
        protected override Endpoint UserInfoServiceEndpoint
        {
            get
            {
                Uri uri = new Uri(SalesforceProfileUrl);
                return new Endpoint
                {
                    BaseUri = uri.GetLeftPart(UriPartial.Authority),
                    Resource = uri.PathAndQuery
                };
            }
        }

        /// <summary>
        /// Friendly name of provider (OAuth2 service).
        /// </summary>
        public override string Name
        {
            get { return "Salesforce"; }
        }

        /// <inheritdoc />
        protected override string ParseTokenResponse(string content, string key)
        {
            // save the user's identity service url which is included in the response
            SalesforceProfileUrl = base.ParseTokenResponse(content, "id");

            return base.ParseTokenResponse(content, key);
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            using var doc = JsonDocument.Parse(content);
            var response = doc.RootElement;
            var photos = response.GetProperty("photos");

            return new UserInfo
            {
                Id = response.GetProperty("id").GetStringValue(),
                Email = response.GetStringOrDefault("email"),
                FirstName = response.GetProperty("first_name").GetString(),
                LastName = response.GetProperty("last_name").GetString(),
                AvatarUri =
                    {
                        Small = photos.GetProperty("thumbnail").GetString(),
                        Normal = photos.GetProperty("picture").GetString(),
                        Large = null
                    }
            };
        }
    }
}