using System.Collections.Generic;
using System.Configuration;

namespace OAuth2.Configuration
{
    /// <summary>
    /// Service settings collection.
    /// </summary>
    public class ServiceCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Returns enumerable collection of client configurations.
        /// </summary>
        public IEnumerable<ClientConfiguration> AsEnumerable()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        /// <summary>
        /// Gets the type of the <see cref="T:System.Configuration.ConfigurationElementCollection"/>.
        /// </summary>
        /// <returns>The <see cref="T:System.Configuration.ConfigurationElementCollectionType"/> of this collection.</returns>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        /// <summary>
        /// Returns settings for service client with given name.
        /// </summary>
        public new ClientConfiguration this[string clientTypeName]
        {
            get { return (ClientConfiguration) BaseGet(clientTypeName); }
        }

        /// <summary>
        /// Returns settings for service client with given index.
        /// </summary>
        public ClientConfiguration this[int index]
        {
            get { return (ClientConfiguration)BaseGet(index); }
        }


        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>
        /// A new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new ClientConfiguration();
        }

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <param name="element">The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.</param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ClientConfiguration) element).ClientTypeName;
        }
    }
}