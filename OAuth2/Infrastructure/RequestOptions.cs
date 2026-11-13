using System;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Transport-level options for HTTP requests made by OAuth clients.
    /// </summary>
    public class RequestOptions
    {
        /// <summary>
        /// Maximum time to wait for a response unless the request specifies its own timeout.
        /// When <c>null</c>, RestSharp's default of 100 seconds applies.
        /// </summary>
        public TimeSpan? Timeout { get; set; }
    }
}
