using System.Collections.Specialized;
using OAuth2.Configuration;
using RestSharp;

namespace OAuth2.Client
{
    /// <summary>
    /// Contains context passed to before/after request hooks during the OAuth authentication flow.
    /// </summary>
    public class BeforeAfterRequestArgs
    {
        /// <summary>
        /// Client instance.
        /// </summary>
        public RestClient Client { get; set; }

        /// <summary>
        /// Request instance.
        /// </summary>
        public RestRequest Request { get; set; }

        /// <summary>
        /// Response instance.
        /// </summary>
        public RestResponse Response { get; set; }

        /// <summary>
        /// Values received from service.
        /// </summary>
        public NameValueCollection Parameters { get; set; }

        /// <summary>
        /// Client configuration.
        /// </summary>
        public IClientConfiguration Configuration { get; set; }
    }
}
