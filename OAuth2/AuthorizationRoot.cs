using System;
using System.Collections.Generic;
using System.Reflection;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using System.Linq;

namespace OAuth2
{
    public class AuthorizationRoot
    {
        private readonly IRequestFactory requestFactory;
        private readonly OAuth2ConfigurationSection configurationSection;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationRoot" /> class.
        /// </summary>
        /// <remarks>
        /// Since this is boundary class, we decided to create 
        /// parameterless constructor where default implementations of dependencies are used.
        /// So, despite we encourage you to employ IoC pattern, 
        /// you are still able to just create instance of manager manually and then use it in your project.
        /// </remarks>
        public AuthorizationRoot() : 
            this(new ConfigurationManager(), "oauth2", new RequestFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationRoot" /> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="configurationSectionName">Name of the configuration section.</param>
        /// <param name="requestFactory">The request factory.</param>
        public AuthorizationRoot(
            IConfigurationManager configurationManager, 
            string configurationSectionName, 
            IRequestFactory requestFactory)
        {
            this.requestFactory = requestFactory;
            configurationSection = configurationManager
                .GetConfigSection<OAuth2ConfigurationSection>(configurationSectionName);
        }

        /// <summary>
        /// Returns collection of clients which were configured 
        /// using application configuration file and are enabled.
        /// </summary>
        public virtual IEnumerable<IClient> Clients
        {
            get
            {
                var types = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(typeof (IClient).IsAssignableFrom).ToList();
                Func<ClientConfiguration, Type> getType = 
                    configuration => types.First(x => x.Name == configuration.ClientTypeName);

                return configurationSection.Services.AsEnumerable()
                    .Where(configuration => configuration.IsEnabled)
                    .Select(configuration => (IClient) Activator.CreateInstance(
                        getType(configuration), requestFactory, configuration));
            }
        }
    }
}