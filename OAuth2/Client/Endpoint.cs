namespace OAuth2.Client
{
    public class Endpoint
    {
        public string BaseUri { get; set; }
        public string Resource { get; set; }
        public string Uri { get { return BaseUri + Resource; } }
    }
}