using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="IRequestFactory"/>.
    /// </summary>
    public class RequestFactory : IRequestFactory
    {
        // Share connections across factories, but keep RestClient request settings separate.
        private static readonly HttpClient SharedHttpClient = CreateSharedHttpClient();
        private readonly RequestOptions? _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestFactory"/> class.
        /// </summary>
        public RequestFactory()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestFactory"/> class with transport options.
        /// </summary>
        /// <param name="options">Transport-level options applied to every client created by this factory.</param>
        public RequestFactory(RequestOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Returns new REST client instance with the specified base URL.
        /// </summary>
        /// <remarks>Disposing the returned client does not dispose the shared transport.</remarks>
        public RestClient CreateClient(string baseUrl)
        {
            if (String.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("Value cannot be null or empty.", nameof(baseUrl));

            var restClientOptions = new RestClientOptions(baseUrl);

            if (_options?.Timeout is not null)
                restClientOptions.Timeout = _options.Timeout.Value;

            return new RestClient(SharedHttpClient, restClientOptions, disposeHttpClient: false);
        }

        /// <summary>
        /// Returns new REST request instance.
        /// </summary>
        public RestRequest CreateRequest(string resource)
        {
            if (String.IsNullOrEmpty(resource))
                throw new ArgumentException("Value cannot be null or empty.", nameof(resource));

            return new RestRequest(resource);
        }

        /// <summary>
        /// Returns new REST request instance with the specified HTTP method.
        /// </summary>
        public RestRequest CreateRequest(string resource, Method method)
        {
            if (String.IsNullOrEmpty(resource))
                throw new ArgumentException("Value cannot be null or empty.", nameof(resource));

            return new RestRequest(resource, method);
        }

        private static HttpClient CreateSharedHttpClient()
        {
            HttpMessageHandler handler;
#if NET8_0_OR_GREATER
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Retire pooled connections periodically so a long-lived client can pick up DNS changes.
                handler = new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = false,
                    AutomaticDecompression = DecompressionMethods.All,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                };
            }
            else
            {
                // Preserve native mobile handlers and browser support.
                var platformHandler = new HttpClientHandler { AllowAutoRedirect = false };
                if (!OperatingSystem.IsBrowser())
                {
                    platformHandler.UseCookies = false;
                    platformHandler.AutomaticDecompression = DecompressionMethods.All;
                }

                handler = platformHandler;
            }
#else
            // Preserve RestSharp's netstandard2.0 defaults and platform handler support.
            handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip
            };
#endif
            // RestSharp applies each request/client timeout with its own cancellation token.
            return new HttpClient(new SameOriginRedirectHandler { InnerHandler = handler }) { Timeout = Timeout.InfiniteTimeSpan };
        }

        private sealed class SameOriginRedirectHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308 && response.Headers.Location is { } location)
                {
                    var source = request.RequestUri!;
                    if (!Uri.TryCreate(source, location, out var target) ||
                        !String.Equals(source.Scheme, target.Scheme, StringComparison.OrdinalIgnoreCase) ||
                        !String.Equals(source.IdnHost, target.IdnHost, StringComparison.OrdinalIgnoreCase) ||
                        source.Port != target.Port)
                    {
                        // Stripping Authorization alone does not protect form/query credentials.
                        response.Dispose();
                        throw new HttpRequestException("OAuth requests cannot follow redirects to another origin.");
                    }
                }

                return response;
            }
        }
    }
}
