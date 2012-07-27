namespace OAuth2.Infrastructure
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
    }
}