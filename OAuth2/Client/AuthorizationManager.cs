using OAuth2.Configuration;
using OAuth2.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OAuth2.Client
{
    public class AuthorizationManager
    {
        private readonly string sectonName;
        private IRequestFactory requestFactory;

        public AuthorizationManager(IRequestFactory requestFactory, string sectionName)
        {
            this.requestFactory = requestFactory;
            this.sectonName = sectionName;
        }

        public virtual IEnumerable<IClient> Clients
        {
            get
            {
                IList<IClient> result = new List<IClient>();
                var configSection = System.Configuration.ConfigurationManager.GetSection(sectonName) as OAuth2ConfigurationSection;

                for (int i = 0; i < configSection.Services.Count; i++)
                {
                    var item = configSection.Services[i];
                    if (item.Enabled)
                    {
                        Type type = Type.GetType(string.Format("{0}.{1}", typeof(OAuth2Client).Namespace, item.ClientTypeName));

                        var ctor = type.GetConstructor(new Type[] { typeof(IRequestFactory), typeof(IClientConfiguration) });
                        IClient client = ctor.Invoke(new object[] { requestFactory, item }) as IClient;
                        if (client != null)
                            result.Add(client);
                    }
                }
                return result;
            }
        }
    }
}
