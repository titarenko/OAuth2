using System.Configuration;
using System.Linq;

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
            get { return this.Services[clientTypeName]; }
        }

        [ConfigurationProperty(CollectionName), ConfigurationCollection(typeof(ServiceCollection))]
        public ServiceCollection Services
        {
            get { return (ServiceCollection) base[CollectionName]; }
        }

        public System.Collections.Generic.IEnumerator<IClientConfiguration> GetEnumerator()
        {
            return Services.AsEnumerable().Cast<IClientConfiguration>().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Services.AsEnumerable().GetEnumerator();
        }
    }
}