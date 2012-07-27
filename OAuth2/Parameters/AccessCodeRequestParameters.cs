namespace OAuth2.Parameters
{
    /// <summary>
    /// Parameters of access code request.
    /// </summary>
    public class AccessCodeRequestParameters
    {
        /// <summary>
        /// OAuth2 provider response type. Should always be "code".
        /// </summary>
        public string ResponseType { get { return "code"; } }

        /// <summary>
        /// Client ID (ID of your application).
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Redirect URI (URI user will be redirected to 
        /// after authentication using third-party service).
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Scope - contains set of permissions which user should give to your application.
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Any arbitrary value which will be given back 
        /// by third-party service after authentication.
        /// </summary>
        public string State { get; set; }
    }
}