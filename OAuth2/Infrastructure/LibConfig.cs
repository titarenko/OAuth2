using System;
using System.Linq;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Library configuration based on <see cref="OAuth2ConfigurationSection"/>.
    /// </summary>
    public class LibConfig : Configuration
    {
        private readonly OAuth2ConfigurationSection configurationSection;
        private readonly ServiceElement serviceElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibConfig"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public LibConfig(IConfigurationManager configurationManager)
        {
            configurationSection = configurationManager
                .GetConfigSection<OAuth2ConfigurationSection>("oauth2");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibConfig"/> class.
        /// </summary>
        /// <param name="serviceElement">The service element.</param>
        public LibConfig(ServiceElement serviceElement)
        {
            this.serviceElement = serviceElement;
        }

        /// <summary>
        /// Returns configuration section by name.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <param name="allowInheritance">Allows read values from parent section if true.</param>
        /// <returns></returns>
        public override IConfiguration GetSection(string name, bool allowInheritance = true)
        {
            if (serviceElement != null)
            {
                throw new NotSupportedException("Nested sections are not supported.");
            }

            return new LibConfig(
                configurationSection.Services
                    .Cast<ServiceElement>()
                    .First(x => x.ClientTypeName == name));
        }

        /// <summary>
        /// Returns value by key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override string Get(string key)
        {
            if (serviceElement == null)
            {
                throw new ApplicationException(
                    "First choose service client and only then obtains its settings.");
            }

            return (string) serviceElement.ElementInformation.Properties[key].Value;
        }
    }
}