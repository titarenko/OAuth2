using OAuth2.Configuration;
using RestSharp.Portable;
using System.Collections.Generic;
using System.Linq;

namespace OAuth2.Client
{
    /// <summary>
    /// Event arguments used before and after a request.
    /// </summary>
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
        public ILookup<string, string> Parameters { get; set; }

        /// <summary>
        /// Client configuration.
        /// </summary>
        public IClientConfiguration Configuration { get; set; }
    }
}