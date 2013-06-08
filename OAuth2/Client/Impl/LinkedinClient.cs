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
        
        /// <summary>
        /// Obtains user information using LinkedIn API.
        /// </summary>
        /// <param name="accessToken">The access token</param>
        /// <returns></returns>
        protected override UserInfo GetUserInfo(string accessToken)
        {
            var client = _factory.NewClient();
            client.BaseUrl = UserInfoServiceEndpoint.BaseUri;
            client.Authenticator = null;

            var request = _factory.NewRequest();
            request.Resource = UserInfoServiceEndpoint.Resource;

            request.Parameters.Add(new Parameter { Name = "oauth2_access_token", Type = ParameterType.GetOrPost, Value = accessToken });

            var result = ParseUserInfo(client.Execute(request).Content);
            result.ProviderName = Name;

            return result;
        }

        /// <summary>
        /// Should return parsed <see cref="UserInfo"/> from content received from third-party service.
        /// </summary>
        /// <param name="content">The content which is received from third-party service.</param>
        protected override UserInfo ParseUserInfo(string content)
        {
            var document = XDocument.Parse(content);

            return new UserInfo
            {
                Id = document.XPathSelectElement("/person/id").Value,
                FirstName = document.XPathSelectElement("/person/first-name").Value,
                LastName = document.XPathSelectElement("/person/last-name").Value,
                PhotoUri = SafeGet(document, "/person/picture-url"),
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