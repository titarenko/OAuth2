using System;
using OAuth2.Configuration;
using OAuth2.Infrastructure;

namespace OAuth2.Client.Impl
{
    /// <summary>
    /// Obsolete. Use <see cref="XClient"/> instead. Twitter has been rebranded to X.
    /// </summary>
    [Obsolete("Use XClient instead. Twitter has been rebranded to X.", true)]
    public class TwitterClient : XClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterClient"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="configuration">The configuration.</param>
        public TwitterClient(IRequestFactory factory, IClientConfiguration configuration)
            : base(factory, configuration)
        {
        }
    }
}
