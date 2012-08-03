using OAuth2.Configuration;
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
        public OAuthSettings GetSetting(string sectionName)
        {
            return System.Configuration.ConfigurationManager.GetSection(sectionName) as OAuthSettings;
        }
    }
}