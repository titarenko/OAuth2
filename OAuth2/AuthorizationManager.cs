using System;
using System.Collections.Generic;
using System.Reflection;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using System.Linq;

namespace OAuth2
{
    public class AuthorizationManager
    {
        private readonly IRequestFactory requestFactory;
        private readonly OAuth2ConfigurationSection configurationSection;

        private IList<IClient> clients;

        public AuthorizationManager() : 
            this(new ConfigurationManager(), "oauth2", new RequestFactory())
        {
        }

        public AuthorizationManager(
            IConfigurationManager configurationManager, 
            string configurationSectionName, 
            IRequestFactory requestFactory)
        {
            this.requestFactory = requestFactory;
            configurationSection = configurationManager
                .GetConfigSection<OAuth2ConfigurationSection>(configurationSectionName);
        }

        public virtual IEnumerable<IClient> Clients
        {
            get
            {
                if (clients == null)
                {
                    var types = Assembly.GetExecutingAssembly().GetTypes()
                        .Where(typeof (IClient).IsAssignableFrom).ToList();
                    Func<ClientConfiguration, Type> getType = 
                        configuration => types.First(x => x.Name == configuration.ClientTypeName);

                    clients = configurationSection.Services.AsEnumerable()
                        .Where(configuration => configuration.IsEnabled)
                        .Select(configuration => (IClient) Activator.CreateInstance(
                            getType(configuration), requestFactory, configuration))
                        .ToList();
                }

                return clients;
            }
        }
    }
}