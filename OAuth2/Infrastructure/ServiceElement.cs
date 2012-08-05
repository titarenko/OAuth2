using System.Configuration;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Contains settings for service client.
    /// </summary>
    public class ServiceElement : ConfigurationElement
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
        [ConfigurationProperty(ScopeKey, IsRequired = true)]
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
            get { return (string)this[RedirectUriKey]; }
        }
    }
}