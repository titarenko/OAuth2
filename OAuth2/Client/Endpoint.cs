namespace OAuth2.Client
{
    /// <summary>
    /// Defines endpoint URI (service address).
    /// </summary>
    public class Endpoint
    {
        /// <summary>
        /// Base URI (service host address).
        /// </summary>
        public string BaseUri { get; set; } = null!;

        /// <summary>
        /// Resource URI (service method).
        /// </summary>
        public string Resource { get; set; } = null!;

        /// <summary>
        /// Complete URI of endpoint (base URI combined with resource URI).
        /// </summary>
        public string Uri { get { return BaseUri + Resource; } }
    }
}
