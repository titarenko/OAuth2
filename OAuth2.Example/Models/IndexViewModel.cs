using System.Collections.Generic;

namespace OAuth2.Example.Models
{
    /// <summary>
    /// View model for index page (view).
    /// </summary>
    public class LoginInfo
    {
        public string NetworkName { get; set; }
        public string Uri { get; set; }
    }

    /// 
    /// 
    public class IndexViewModel
    {
        /// <summary>
        /// Collection of log in URIs for all registered providers.
        /// </summary>
        public IEnumerable<LoginInfo> LoginInfos { get; set; }
    }
}