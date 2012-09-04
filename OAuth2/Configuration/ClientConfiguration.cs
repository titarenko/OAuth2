using System;
using System.Configuration;
using System.Web;

namespace OAuth2.Configuration
{
    /// <summary>
    /// Contains settings for service client.
    /// </summary>
    public class ClientConfiguration : ConfigurationElement, IClientConfiguration
    {
        private const string ClientTypeNameKey = "clientType";
        private const string ClientIdKey = "clientId";
        private const string ClientSecretKey = "clientSecret";
        private const string ScopeKey = "scope";
        private const string RedirectUriKey = "redirectUri";

        /// <summary>
        /// Name of client type.
        /// </summary>
        [ConfigurationProperty(ClientTypeNameKey, IsRequired = true, IsKey = true)]
        public string ClientTypeName
        {
            get { return (string) this[ClientTypeNameKey]; }
        }
        
        /// <summary>
        /// Client ID (ID of your application).
        /// </summary>
        [ConfigurationProperty(ClientIdKey, IsRequired = true)]
        public string ClientId
        {
            get { return (string) this[ClientIdKey]; }
        }

        /// <summary>
        /// Client secret.
        /// </summary>
        [ConfigurationProperty(ClientSecretKey, IsRequired = true)]
        public string ClientSecret
        {
            get { return (string) this[ClientSecretKey]; }
        }

        /// <summary>
        /// Scope - contains set of permissions which user should give to your application.
        /// </summary>
        [ConfigurationProperty(ScopeKey)]
        public string Scope
        {
            get { return (string) this[ScopeKey]; }
        }

        /// <summary>
        /// Redirect URI (URI user will be redirected to 
        /// after authentication using third-party service).
        /// </summary>
        [ConfigurationProperty(RedirectUriKey, IsRequired = true)]
        public string RedirectUri
        {
            get
            {
                return GetFullUri((string) this[RedirectUriKey]);
            }
        }

        private string GetFullUri(string uri)
        {
            if (!uri.StartsWith("~"))
            {
                return uri;
            }

            if (HttpContext.Current == null)
            {
                throw new ApplicationException(
                    "Cannot resolve relative URI outside of ASP.NET application " +
                    "(current HTTP context is NULL).");
            }

            return HttpContext.Current.Request.ApplicationPath + uri.Substring(1);
        }
    }
}