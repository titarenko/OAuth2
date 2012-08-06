namespace OAuth2.Configuration
{
    /// <summary>
    /// Wrapper for <see cref="System.Configuration.ConfigurationManager"/>.
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
        /// <summary>
        /// Returns value obtained from
        /// <see cref="System.Configuration.ConfigurationManager.AppSettings"/> using specified key.
        /// </summary>
        public string GetAppSetting(string key)
        {
            return System.Configuration.ConfigurationManager.AppSettings[key];
        }

        /// <summary>
        /// Returns configuration section object model.
        /// </summary>
        /// <typeparam name="T">Type representing root of configuration section.</typeparam>
        /// <param name="name">Name of configuration section.</param>
        public T GetConfigSection<T>(string name)
        {
            return (T) System.Configuration.ConfigurationManager.GetSection(name);
        }
    }
}