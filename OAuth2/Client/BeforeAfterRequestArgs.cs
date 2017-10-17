using System.Collections.Specialized;
using OAuth2.Configuration;
using RestSharpInternal;

namespace OAuth2.Client
{
    public class BeforeAfterRequestArgs
    {
        /// <summary>
        /// Client instance.
        /// </summary>
        public IRestClient Client { get; set; }

        /// <summary>
        /// Request instance.
        /// </summary>
        public IRestRequest Request { get; set; }

        /// <summary>
        /// Response instance.
        /// </summary>
        public IRestResponse Response { get; set; }

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