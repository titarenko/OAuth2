namespace OAuth2.Configuration
{
    /// <summary>
    /// API to access OAuth2 provider configurations
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Returns an application setting value
        /// </summary>
        /// <param name="key">Key of the application setting</param>
        string GetAppSetting(string key);

        /// <summary>
        /// Returns configuration section object model.
        /// </summary>
        /// <typeparam name="T">Type representing root of configuration section.</typeparam>
        /// <param name="name">Name of configuration section.</param>
        T GetConfiguration<T>(string name);
    }
}