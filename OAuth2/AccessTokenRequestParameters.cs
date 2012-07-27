namespace OAuth2
{
    public class AccessTokenRequestParameters
    {
        public string Code { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        //public string Scope { get; set; }
        public string GrantType { get { return "authorization_code"; } }
        //public string ResponseType { get { return "token"; } }
    }
}