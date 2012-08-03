using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace OAuth2.Configuration
{
    public class OAuthSettings : ConfigurationSection
    {
        [ConfigurationProperty("networks", IsDefaultCollection = false),
        ConfigurationCollection(typeof(NetworkCollection), AddItemName = "addNetwork", ClearItemsName = "clearNetworks", RemoveItemName = "removeNetwork")]
        public NetworkCollection Networks
        {
            get { return this["networks"] as NetworkCollection; }
        }
    }
}
