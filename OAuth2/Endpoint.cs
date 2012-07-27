namespace OAuth2
{
    public class Endpoint
    {
        public string BaseUri { get; set; }
        public string Resource { get; set; }
        public string Uri { get { return BaseUri + Resource; } }
    }
}