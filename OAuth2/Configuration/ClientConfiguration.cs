using System.Configuration;
using OAuth2.Infrastructure;

namespace OAuth2.Configuration
{
    /// <summary>
    /// Contains settings for service client.
    /// </summary>
    public class ClientConfiguration : ConfigurationElement, IClientConfiguration
    {
        private const string ClientTypeNameKey = "clientType";
        private const string ClientIdKey = "clientId";
        private const string EnabledKey = "enabled";
        private const string ClientSecretKey = "clientSecret";
        private const string ClientPublicKey = "clientPublic";
        private const string ScopeKey = "scope";
        private const string RedirectUriKey = "redirectUri";

        /// <summary>
        /// Name of client type.
        /// </summary>
        [ConfigurationProperty(ClientTypeNameKey, IsRequired = true, IsKey = true)]
        public string ClientTypeName
        {
            get { return (string) this[ClientTypeNameKey]; }
            set { this[ClientTypeNameKey] = value; }
        }
        
        /// <summary>
        /// Client ID (ID of your application).
        /// </summary>
        [ConfigurationProperty(ClientIdKey, IsRequired = true)]
        public string ClientId
        {
            get { return (string) this[ClientIdKey]; }
            set { this[ClientIdKey] = value; }
        }

        /// <summary>
        /// Client state: enabled or disabled.
        /// </summary>
        [ConfigurationProperty(EnabledKey, DefaultValue = true)]
        public bool IsEnabled
        {
            get { return (bool)this[EnabledKey]; }
            set { this[EnabledKey] = value; }
        }

        /// <summary>
        /// Client secret.
        /// </summary>
        [ConfigurationProperty(ClientSecretKey, IsRequired = true)]
        public string ClientSecret
        {
            get { return (string) this[ClientSecretKey]; }
            set { this[ClientSecretKey] = value; }
        }

        /// <summary>
        /// Client secret.
        /// </summary>
        [ConfigurationProperty(ClientPublicKey)]
        public string ClientPublic
        {
            get { return (string)this[ClientPublicKey]; }
            set { this[ClientPublicKey] = value; }
        }

        /// <summary>
        /// Scope - contains set of permissions which user should give to your application.
        /// </summary>
        [ConfigurationProperty(ScopeKey)]
        public string Scope
        {
            get { return (string) this[ScopeKey]; }
            set { this[ScopeKey] = value; }
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
                return UriUtility.ToAbsolute((string) this[RedirectUriKey]);
            }
            set
            {
            	this[RedirectUriKey] = value;
            }
        }
    }
}