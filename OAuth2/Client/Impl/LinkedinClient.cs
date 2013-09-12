using System;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using System.Xml.Linq;
using System.Xml.XPath;

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
                    BaseUri = "https://www.linkedin.com",
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
                    BaseUri = "https://www.linkedin.com",
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
                    BaseUri = "https://api.linkedin.com",
                    Resource = "/v1/people/~:(id,first-name,last-name,picture-url)"
                };
            }
        }

        public override string GetLoginLinkUri(string state = null)
        {
            return base.GetLoginLinkUri(state ?? Guid.NewGuid().ToString("N"));
        }

        protected override void BeforeGetUserInfo(BeforeAfterRequestArgs args)
        {
            args.Client.Authenticator = null;
            args.Request.Parameters.Add(new Parameter
            {
                Name = "oauth2_access_token",
                Type = ParameterType.GetOrPost,
                Value = AccessToken
            });
        }
        
        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var document = XDocument.Parse(content);
            var avatarUri = SafeGet(document, "/person/picture-url");            
            const string avatarSizeTemplate = "shrink_{0}_{0}";
            var avatarDefaultSize =  string.Format("shrink_{0}_{0}", 80);
            
            return new UserInfo
            {
                Id = document.XPathSelectElement("/person/id").Value,
                FirstName = document.XPathSelectElement("/person/first-name").Value,
                LastName = document.XPathSelectElement("/person/last-name").Value,
                AvatarUri =
                    {
                        Small =  avatarUri.Replace(avatarDefaultSize, string.Format(avatarSizeTemplate, AvatarInfo.SmallSize)),
                        Normal = avatarUri,
                        Large = avatarUri.Replace(avatarDefaultSize, string.Format(avatarSizeTemplate, AvatarInfo.LargeSize))
                    }
            };            
        }


        private string SafeGet(XDocument document, string path)
        {
            var element = document.XPathSelectElement(path);
            if (element == null)
                return null;

            return element.Value;
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