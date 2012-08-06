namespace OAuth2.Configuration
{
    /// <summary>
    /// Defines API for <see cref="System.Configuration.ConfigurationManager"/> wrapper.
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Returns value obtained from 
        /// <see cref="System.Configuration.ConfigurationManager.AppSettings"/> using specified key.
        /// </summary>
        string GetAppSetting(string key);

        /// <summary>
        /// Returns configuration section object model.
        /// </summary>
        /// <typeparam name="T">Type representing root of configuration section.</typeparam>
        /// <param name="name">Name of configuration section.</param>
        T GetConfigSection<T>(string name);
    }
}