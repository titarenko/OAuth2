namespace OAuth2.Parameters
{
    /// <summary>
    /// Parameters of access token request.
    /// </summary>
    public class AccessTokenRequestParameters
    {
        /// <summary>
        /// Code which was received from third-party authentication service.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Client ID (ID of your application).
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret.
        /// </summary>
        public string ClientSecret { get; set; }

        //public string RedirectUri { get; set; }

        /// <summary>
        /// Grant type. Should always be "authorization_code".
        /// </summary>
        public string GrantType { get { return "authorization_code"; } }
    }
}