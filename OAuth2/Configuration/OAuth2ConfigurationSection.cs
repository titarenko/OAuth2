using System.Configuration;

namespace OAuth2.Configuration
{
    /// <summary>
    /// Library configuration section handler.
    /// </summary>
    public class OAuth2ConfigurationSection : ConfigurationSection, IOAuth2Configuration
    {
        private const string CollectionName = "services";

        /// <summary>
        /// Returns settings for service client with given name.
        /// </summary>
        public new IClientConfiguration this[string clientTypeName]
        {
            get { return Services[clientTypeName]; }
        }

        [ConfigurationProperty(CollectionName), ConfigurationCollection(typeof(ServiceCollection))]
        public ServiceCollection Services
        {
            get { return (ServiceCollection) base[CollectionName]; }
        }
    }
}