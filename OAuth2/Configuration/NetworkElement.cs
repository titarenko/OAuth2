using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace OAuth2.Configuration
{
    public class NetworkElement : ConfigurationElement
    {
        public NetworkElement() { }

        public NetworkElement(string clientType, string clientId, string clientSecret, string scope, string redirectUri)
        {
            this.ClientType = clientType;
            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.Scope = scope;
            this.RedirectUri = redirectUri;
        }

        [ConfigurationProperty("clientType", IsRequired = true)]
        public string ClientType
        {
            get
            {
                return (string)this["clientType"];
            }
            set
            {
                this["clientType"] = value;
            }
        }


        [ConfigurationProperty("clientId", IsRequired = true)]
        public string ClientId
        {
            get
            {
                return (string)this["clientId"];
            }
            set
            {
                this["clientId"] = value;
            }
        }

        [ConfigurationProperty("clientSecret", IsRequired = true)]
        public string ClientSecret
        {
            get
            {
                return (string)this["clientSecret"];
            }
            set
            {
                this["clientSecret"] = value;
            }
        }

        [ConfigurationProperty("scope", IsRequired = true)]
        public string Scope
        {
            get
            {
                return (string)this["scope"];
            }
            set
            {
                this["scope"] = value;
            }
        }

        [ConfigurationProperty("redirectUri", IsRequired = true)]
        public string RedirectUri
        {
            get { return (string)this["redirectUri"]; }
            set { this["redirectUri"] = value; }
        }

    }
}
