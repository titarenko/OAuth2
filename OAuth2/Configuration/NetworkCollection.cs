using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace OAuth2.Configuration
{
    public class NetworkCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        public NetworkElement this[int index]
        {
            get { return (NetworkElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public void Add(NetworkElement element)
        {
            BaseAdd(element);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new NetworkElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NetworkElement)element).ClientType.ToString();
        }

        public void Remove(NetworkElement element)
        {
            BaseRemove(element.ClientType.ToString());
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }
    }

}
