namespace OAuth2.Configuration
{
    /// <summary>
    /// Library configuration section handler.
    /// </summary>
    public interface IOAuth2ConfigurationSection
    {
        /// <summary>
        /// Returns settings for service client with given name.
        /// </summary>
        IClientConfiguration this[string clientTypeName] { get; }

        /// <summary>
        /// Gets the services.
        /// </summary>
        ServiceCollection Services { get; }
    }
}