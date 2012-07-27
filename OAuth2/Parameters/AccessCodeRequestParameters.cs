namespace OAuth2.Parameters
{
    public class AccessCodeRequestParameters
    {
        public string ResponseType { get { return "code"; } }
        public string ClientId { get; set; }
        public string RedirectUri { get; set; }
        public string Scope { get; set; }
        public string State { get; set; }
    }
}