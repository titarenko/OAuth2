using System.Collections.Generic;

namespace OAuth2.Configuration
{
    /// <summary>
    /// OAuth2 library configuration.
    /// </summary>
    public interface IOAuth2Configuration : IEnumerable<IClientConfiguration>
    {
        /// <summary>
        /// Returns settings for service client with given name.
        /// </summary>
        IClientConfiguration this[string clientTypeName] { get; }
   }
}