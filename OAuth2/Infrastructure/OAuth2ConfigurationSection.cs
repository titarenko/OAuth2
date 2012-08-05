using System.Configuration;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Library configuration section handler.
    /// </summary>
    public class OAuth2ConfigurationSection : ConfigurationSection
    {
        private const string CollectionName = "services";

        /// <summary>
        /// Collection of settings defined per service.
        /// </summary>
        [ConfigurationProperty(CollectionName), ConfigurationCollection(typeof(ServiceCollection))]
        public ServiceCollection Services
        {
            get { return (ServiceCollection) this[CollectionName]; }
        }
    }
}