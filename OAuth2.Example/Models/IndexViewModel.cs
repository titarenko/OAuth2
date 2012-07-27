using System.Collections.Generic;

namespace OAuth2.Example.Models
{
    /// <summary>
    /// View model for index page (view).
    /// </summary>
    public class IndexViewModel
    {
        /// <summary>
        /// Collection of log in URIs for all registered providers.
        /// </summary>
        public IEnumerable<string> LoginUris { get; set; }
    }
}