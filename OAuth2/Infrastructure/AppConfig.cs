using System;
using System.Configuration;
using System.Linq;
using OAuth2.Configuration;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Implementation of <see cref="IConfiguration"/> which is based on <see cref="ConfigurationManager"/>.
    /// </summary>
    public class AppConfig : IConfiguration
    {
        private readonly IConfigurationManager configurationManager;
        private readonly string sectionName;
        private readonly bool allowInheritance;
        private readonly string configSectionName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfig"/> class.
        /// </summary>
        public AppConfig(IConfigurationManager configurationManager, string configSectionName)
        {
            this.configurationManager = configurationManager;
            this.configSectionName = configSectionName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfig"/> class.
        /// </summary>
        private AppConfig(IConfigurationManager configurationManager, string sectionName, bool allowInheritance, string configSectionName)
            : this(configurationManager, configSectionName)
        {
            this.sectionName = sectionName;
            this.allowInheritance = allowInheritance;
        }

        /// <summary>
        /// Returns configuration section by name.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <param name="allowInheritance">Allows read values from parent section if true.</param>
        public IConfiguration GetSection(string name, bool allowInheritance = true)
        {
            return new AppConfig(configurationManager, name, allowInheritance, configSectionName);
        }

        /// <summary>
        /// Returns configuration section for given type (uses type name as section name).
        /// </summary>
        /// <param name="allowInheritance">Allows read values from parent section if true.</param>
        public IConfiguration GetSection<T>(bool allowInheritance = true)
        {
            return GetSection(typeof(T), allowInheritance);
        }

        /// <summary>
        /// Returns configuration section for given type (uses type name as section name).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="allowInheritance">Allows read values from parent section if true.</param>
        /// <returns></returns>
        public IConfiguration GetSection(Type type, bool allowInheritance = true)
        {
            return GetSection(type.Name, allowInheritance);
        }

        /// <summary>
        /// Returns value by key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            OAuthSettings settings = configurationManager.GetSetting(configSectionName);
            foreach (NetworkElement item in settings.Networks)
            {
                if (item.ClientType.Equals(sectionName))
                {
                    string propKey = Char.ToLowerInvariant(key[0]) + key.Substring(1);
                    PropertyInformation pi = item.ElementInformation.Properties[propKey];
                    if (pi != null)
                        return Convert.ToString(pi.Value);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Returns instance with properties initialized from configuration values.
        /// </summary>
        public T Get<T>()
        {
            var instance = Activator.CreateInstance<T>();
            typeof(T).GetProperties()
                .Where(x => x.CanWrite)
                .ForEach(x => x.SetValue(instance, Get(x.Name, x.PropertyType), null));
            return instance;
        }

        /// <summary>
        /// Returns strongly typed value by key.
        /// </summary>
        public T Get<T>(string key)
        {
            return (T)Get(key, typeof(T));
        }

        private object Get(string key, Type valueType)
        {
            return Convert.ChangeType(Get(key), valueType);
        }
    }
}