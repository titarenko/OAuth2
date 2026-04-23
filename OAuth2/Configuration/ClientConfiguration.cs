namespace OAuth2.Configuration
{
    /// <summary>
    /// Runtime client configuration. 
    /// Use instead of <see cref="ClientConfiguration"/> when you need manually create client configuration in runtime.
    /// </summary>
    public class ClientConfiguration : IClientConfiguration
    {
        /// <summary>
        /// Name of client type.
        /// </summary>
        public string ClientTypeName { get; set; } = null!;

        /// <summary>
        /// Client state: enabled or disabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Client ID (ID of your application).
        /// </summary>
        public string ClientId { get; set; } = null!;

        /// <summary>
        /// Client secret.
        /// </summary>
        public string ClientSecret { get; set; } = null!;

        /// <summary>
        /// Public key.
        /// </summary>
        public string? ClientPublic { get; set; }

        /// <summary>
        /// Scope - contains set of permissions which user should give to your application.
        /// </summary>
        public string? Scope { get; set; }

        /// <summary>
        /// Redirect URI (URI user will be redirected to
        /// after authentication using third-party service).
        /// </summary>
        public string RedirectUri { get; set; } = null!;
    }
}
