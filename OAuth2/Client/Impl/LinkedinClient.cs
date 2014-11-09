using System;
using System.Linq;
using System.Xml.Linq;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Portable;
using System.Threading.Tasks;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// LinkedIn authentication client.
    /// </summary>
    public class LinkedInClient : OAuth2Client
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedInClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public LinkedInClient(IRequestFactory factory, IClientConfiguration configuration)
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
                    BaseUri  = "https://www.linkedin.com",
                    Resource = "/uas/oauth2/authorization"
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
                    BaseUri  = "https://www.linkedin.com",
                    Resource = "/uas/oauth2/accessToken"
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
                    BaseUri  = "https://api.linkedin.com",
                    Resource = "/v1/people/~:(id,email-address,first-name,last-name,picture-url)"
                };
            }
        }

        public override async Task<string> GetLoginLinkUri(string state = null)
        {
            return await base.GetLoginLinkUri(state ?? Guid.NewGuid().ToString("N"));
        }

        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Client.Authenticator = null;
            args.Request.Parameters.Add(new Parameter
            {
                Name  = "oauth2_access_token",
                Type  = ParameterType.GetOrPost,
                Value = AccessToken
            });
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var document  = XDocument.Parse(content);
            var avatarUri = document.Element("person").Elements("picture-url").Select(x => x.Value).SingleOrDefault();
            var avatarSizeTemplate = "{0}_{0}";
            if (string.IsNullOrEmpty(avatarUri))
            {
                avatarUri = "https://www.linkedin.com/scds/common/u/images/themes/katy/ghosts/person/ghost_person_80x80_v1.png";
                avatarSizeTemplate = "{0}x{0}";
            }
            var avatarDefaultSize = string.Format(avatarSizeTemplate, 80);

            return new UserInfo
            {
                Id        = document.Element("person").Element("id").Value,
                Email     = document.Element("person").Elements("email-address").Select(x => x.Value).SingleOrDefault(),
                FirstName = document.Element("person").Element("first-name").Value,
                LastName = document.Element("person").Element("last-name").Value,
                AvatarUri =
                    {
                        Small  = avatarUri.Replace(avatarDefaultSize, string.Format(avatarSizeTemplate, AvatarInfo.SmallSize)),
                        Normal = avatarUri,
                        Large  = avatarUri.Replace(avatarDefaultSize, string.Format(avatarSizeTemplate, AvatarInfo.LargeSize))
                    }
            };
        }


        /// <summary>
        /// Friendly name of provider (OAuth service).
        /// </summary>
        public override string Name
        {
            get { return "LinkedIn"; }
        }
    }
}
