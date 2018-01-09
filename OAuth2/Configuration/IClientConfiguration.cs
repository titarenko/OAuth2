namespace OAuth2.Configuration
{
    /// <summary>
    /// Configuration of third-party authentication service client.
    /// </summary>
    public interface IClientConfiguration
    {
        /// <summary>
        /// Name of client type.
        /// </summary>
        string ClientTypeName { get; set; }

        /// <summary>
        /// Client state: enabled or disabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Client ID (ID of your application).
        /// </summary>
        string ClientId { get; set; }

        /// <summary>
        /// Client secret.
        /// </summary>
        string ClientSecret { get; set; }

        /// <summary>
        /// Public key.
        /// </summary>
        string ClientPublic { get; set; }

        /// <summary>
        /// Scope - contains set of permissions which user should give to your application.
        /// </summary>
        string Scope { get; set; }

        /// <summary>
        /// Redirect URI (URI user will be redirected to 
        /// after authentication using third-party service).
        /// </summary>
        string RedirectUri { get; set; }
    }
}