using System;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Transport-level options for HTTP requests made by OAuth clients.
    /// </summary>
    public class RequestOptions
    {
        /// <summary>
        /// Maximum time to wait for a response. When <c>null</c>, the HTTP client default is used.
        /// </summary>
        public TimeSpan? Timeout { get; set; }
    }
}
