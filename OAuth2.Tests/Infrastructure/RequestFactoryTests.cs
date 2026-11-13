using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Infrastructure;
using RestSharp;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class RequestFactoryTests
    {
        private RequestFactory _factory = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = new RequestFactory();
        }

        [Test]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            // The factory is initialized in SetUp.

            // Act & Assert
            FluentActions.Invoking(() => new RequestFactory(null!))
                .Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Test]
        public void CreateClient_CalledTwice_ReturnsNewInstances()
        {
            // Arrange
            // The factory is initialized in SetUp.

            // Act
            using var client1 = _factory.CreateClient("https://localhost");
            using var client2 = _factory.CreateClient("https://localhost");

            // Assert
            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client1.Should().NotBeSameAs(client2);
        }

        [Test]
        public async Task CreateClient_ConcurrentWrappers_IsolatesCredentials()
        {
            // Arrange
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var baseUrl = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            var bothReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var receivedCount = 0;
            using var first = _factory.CreateClient(baseUrl);
            using var second = new RequestFactory().CreateClient(baseUrl);
            first.AddDefaultHeader("Authorization", "Bearer first-test-token");
            second.AddDefaultHeader("Authorization", "Bearer second-test-token");
            first.AddDefaultHeader("X-Flow", "first");
            second.AddDefaultHeader("X-Flow", "second");

            async Task<string> ServeRequestAsync()
            {
                using var connection = await listener.AcceptTcpClientAsync(deadline.Token);
                using var stream = connection.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                var lines = new List<string>();
                string? line;
                while ((line = await reader.ReadLineAsync(deadline.Token)) != "")
                {
                    if (line is null)
                        throw new EndOfStreamException("The request ended before its headers.");
                    lines.Add(line);
                }

                if (Interlocked.Increment(ref receivedCount) == 2)
                    bothReceived.TrySetResult(true);
                // Neither response completes until both differently authenticated requests are in flight.
                await bothReceived.Task.WaitAsync(deadline.Token);
                await stream.WriteAsync(CreateResponse("Connection: close\r\n", false).AsMemory(), deadline.Token);
                return String.Join("\r\n", lines);
            }

            var firstServed = ServeRequestAsync();
            var secondServed = ServeRequestAsync();

            // Act
            var responses = await Task.WhenAll(
                first.ExecuteAsync(new RestRequest("/first"), deadline.Token),
                second.ExecuteAsync(new RestRequest("/second"), deadline.Token));
            var requests = await Task.WhenAll(firstServed, secondServed);

            // Assert
            responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
            var firstRequest = requests.Should().ContainSingle(request => request.StartsWith("GET /first HTTP/1.1")).Which;
            firstRequest.Should().Contain("Authorization: Bearer first-test-token");
            firstRequest.Should().Contain("X-Flow: first");
            firstRequest.Should().NotContain("second-test-token");
            firstRequest.Should().NotContain("X-Flow: second");
            var secondRequest = requests.Should().ContainSingle(request => request.StartsWith("GET /second HTTP/1.1")).Which;
            secondRequest.Should().Contain("Authorization: Bearer second-test-token");
            secondRequest.Should().Contain("X-Flow: second");
            secondRequest.Should().NotContain("first-test-token");
            secondRequest.Should().NotContain("X-Flow: first");
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CreateClient_PendingResponse_PreservesTimeoutAndCancellation(bool cancelRequest)
        {
            // Arrange
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
            var accepted = listener.AcceptTcpClientAsync(deadline.Token);
            var baseUrl = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            var factory = new RequestFactory(new RequestOptions { Timeout = TimeSpan.FromSeconds(1) });
            using var client = factory.CreateClient(baseUrl);

            // Act
            var pending = client.ExecuteAsync(new RestRequest("/pending"), cancellation.Token);
            using var connection = await accepted;
            if (cancelRequest)
                cancellation.Cancel();
            var response = await pending;

            // Assert
            response.ResponseStatus.Should().Be(cancelRequest ? ResponseStatus.Aborted : ResponseStatus.TimedOut);
        }

        [TestCase(false, Method.Get, 301)]
        [TestCase(false, Method.Get, 302)]
        [TestCase(false, Method.Get, 303)]
        [TestCase(false, Method.Get, 307)]
        [TestCase(false, Method.Get, 308)]
        [TestCase(false, Method.Post, 301)]
        [TestCase(false, Method.Post, 302)]
        [TestCase(false, Method.Post, 303)]
        [TestCase(false, Method.Post, 307)]
        [TestCase(false, Method.Post, 308)]
        [TestCase(true, Method.Get, 301)]
        [TestCase(true, Method.Get, 302)]
        [TestCase(true, Method.Get, 303)]
        [TestCase(true, Method.Get, 307)]
        [TestCase(true, Method.Get, 308)]
        [TestCase(true, Method.Post, 301)]
        [TestCase(true, Method.Post, 302)]
        [TestCase(true, Method.Post, 303)]
        [TestCase(true, Method.Post, 307)]
        [TestCase(true, Method.Post, 308)]
        public async Task CreateClient_Redirect_RestrictsCredentialsToOriginalOrigin(bool crossOrigin, Method method, int statusCode)
        {
            // Arrange
            using var origin = new TcpListener(IPAddress.Loopback, 0);
            using var otherOrigin = new TcpListener(IPAddress.Loopback, 0);
            origin.Start();
            otherOrigin.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var baseUrl = "http://127.0.0.1:" + ((IPEndPoint)origin.LocalEndpoint).Port;
            var location = crossOrigin
                ? "http://127.0.0.1:" + ((IPEndPoint)otherOrigin.LocalEndpoint).Port + "/redirected"
                : "/redirected";
            var redirect = Encoding.ASCII.GetBytes("HTTP/1.1 " + statusCode + " Redirect\r\nContent-Length: 0\r\nLocation: " + location + "\r\n\r\n");
            var responses = crossOrigin ? new[] { redirect } : new[] { redirect, CreateResponse("Connection: close\r\n", false) };
            var served = ServeResponsesAsync(origin, responses, deadline.Token);
            var factory = new RequestFactory(new RequestOptions { Timeout = TimeSpan.FromSeconds(2) });
            using var client = factory.CreateClient(baseUrl);
            var request = new RestRequest("/initial", method);
            if (method == Method.Post)
                request.AddParameter("client_secret", "test-secret");
            else
                request.AddQueryParameter("access_token", "test-token");
            request.AddHeader("Authorization", "Bearer test-token");

            // Act
            var response = await client.ExecuteAsync(request, deadline.Token);
            var requests = await served;

            // Assert
            otherOrigin.Pending().Should().BeFalse("credentials must never reach the other origin");
            if (crossOrigin)
            {
                requests.Should().ContainSingle();
                response.ResponseStatus.Should().Be(ResponseStatus.Error);
                response.ErrorException.Should().BeOfType<HttpRequestException>()
                    .Which.Message.Should().Be("OAuth requests cannot follow redirects to another origin.");
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                requests.Should().HaveCount(2);
                requests[1].Should().NotContain("Authorization:");
                if (method == Method.Get)
                {
                    requests[1].Should().StartWith("GET /redirected?access_token=test-token HTTP/1.1");
                }
                else if (statusCode is 307 or 308)
                {
                    requests[1].Should().StartWith("POST /redirected HTTP/1.1");
                    requests[1].Should().Contain("client_secret=test-secret");
                }
                else
                {
                    requests[1].Should().StartWith("GET /redirected HTTP/1.1");
                    requests[1].Should().NotContain("client_secret");
                }
            }
        }

        [TestCase("http://localhost:{0}/redirected")]
        [TestCase("https://127.0.0.1:{0}/redirected")]
        [TestCase("//localhost:{0}/redirected")]
        [TestCase("http://127.0.0.1:{0}@localhost:{0}/redirected")]
        public async Task CreateClient_RedirectAuthority_RejectsDifferentHostOrScheme(string locationTemplate)
        {
            // Arrange
            using var origin = new TcpListener(IPAddress.Loopback, 0);
            origin.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var port = ((IPEndPoint)origin.LocalEndpoint).Port;
            var location = String.Format(locationTemplate, port);
            var redirect = Encoding.ASCII.GetBytes("HTTP/1.1 307 Temporary Redirect\r\nContent-Length: 0\r\nLocation: " + location + "\r\n\r\n");
            var served = ServeResponsesAsync(origin, new[] { redirect }, deadline.Token);
            using var client = _factory.CreateClient("http://127.0.0.1:" + port);
            var request = new RestRequest("/initial").AddQueryParameter("access_token", "test-token");

            // Act
            var response = await client.ExecuteAsync(request, deadline.Token);
            var requests = await served;

            // Assert
            requests.Should().ContainSingle();
            origin.Pending().Should().BeFalse("even aliases of the same loopback address are a different origin");
            response.ResponseStatus.Should().Be(ResponseStatus.Error);
            response.ErrorException.Should().BeOfType<HttpRequestException>()
                .Which.Message.Should().Be("OAuth requests cannot follow redirects to another origin.");
        }

        [TestCase(Method.Get)]
        [TestCase(Method.Post)]
        public async Task CreateClient_RedirectChain_RejectsExternalHopAndRemainsUsable(Method method)
        {
            // Arrange
            using var origin = new TcpListener(IPAddress.Loopback, 0);
            using var otherOrigin = new TcpListener(IPAddress.Loopback, 0);
            origin.Start();
            otherOrigin.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var baseUrl = "http://127.0.0.1:" + ((IPEndPoint)origin.LocalEndpoint).Port;
            var location = "//127.0.0.1:" + ((IPEndPoint)otherOrigin.LocalEndpoint).Port + "/external";
            var firstRedirect = Encoding.ASCII.GetBytes("HTTP/1.1 307 Temporary Redirect\r\nContent-Length: 0\r\nLocation: /next\r\nSet-Cookie: session=first; Path=/\r\n\r\n");
            var secondRedirect = Encoding.ASCII.GetBytes("HTTP/1.1 308 Permanent Redirect\r\nContent-Length: 0\r\nLocation: " + location + "\r\n\r\n");
            var served = ServeResponsesAsync(origin, new[] { firstRedirect, secondRedirect, CreateResponse("Connection: close\r\n", false) }, deadline.Token);
            var factory = new RequestFactory(new RequestOptions { Timeout = TimeSpan.FromSeconds(2) });
            using var client = factory.CreateClient(baseUrl);
            var request = new RestRequest("/initial", method);
            if (method == Method.Post)
                request.AddParameter("client_secret", "test-secret");
            else
                request.AddQueryParameter("access_token", "test-token");

            // Act
            var response = await client.ExecuteAsync(request, deadline.Token);
            var recovery = await client.ExecuteAsync(new RestRequest("/recovery"), deadline.Token);
            var requests = await served;

            // Assert
            otherOrigin.Pending().Should().BeFalse("a permitted first hop must not authorize the next origin");
            response.ResponseStatus.Should().Be(ResponseStatus.Error);
            response.ErrorException.Should().BeOfType<HttpRequestException>()
                .Which.Message.Should().Be("OAuth requests cannot follow redirects to another origin.");
            recovery.StatusCode.Should().Be(HttpStatusCode.OK);
            requests.Should().HaveCount(3);
            requests[1].Should().Contain(method == Method.Post ? "client_secret=test-secret" : "access_token=test-token");
            requests[1].Should().Contain("Cookie: session=first");
            requests[2].Should().StartWith("GET /recovery HTTP/1.1");
            requests[2].Should().NotContain("Cookie:");
            requests[2].Should().NotContain("test-secret");
            requests[2].Should().NotContain("test-token");
        }

        [TestCase(null)]
        [TestCase("http://[")]
        public async Task CreateClient_RedirectWithoutUsableLocation_RejectsResponse(string? location)
        {
            // Arrange
            using var origin = new TcpListener(IPAddress.Loopback, 0);
            origin.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var locationHeader = location is null ? "" : "Location: " + location + "\r\n";
            var redirect = Encoding.ASCII.GetBytes("HTTP/1.1 307 Temporary Redirect\r\nContent-Length: 0\r\n" + locationHeader + "\r\n");
            var served = ServeResponsesAsync(origin, new[] { redirect }, deadline.Token);
            using var client = _factory.CreateClient("http://127.0.0.1:" + ((IPEndPoint)origin.LocalEndpoint).Port);
            var request = new RestRequest("/initial").AddQueryParameter("access_token", "test-token");

            // Act & Assert
            await client.Awaiting(x => x.ExecuteAndVerifyAsync(request, deadline.Token))
                .Should().ThrowAsync<UnexpectedResponseException>();
            (await served).Should().ContainSingle();
            origin.Pending().Should().BeFalse("an unusable Location must not cause a follow-up request");
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CreateClient_RequestTimeout_OverridesFactoryTimeout(bool infiniteFactoryTimeout)
        {
            // Arrange
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var accepted = listener.AcceptTcpClientAsync(deadline.Token);
            var baseUrl = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            var factory = new RequestFactory(new RequestOptions
            {
                Timeout = infiniteFactoryTimeout ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(30)
            });
            using var client = factory.CreateClient(baseUrl);
            var request = new RestRequest("/pending") { Timeout = TimeSpan.FromSeconds(1) };

            // Act
            var pending = client.ExecuteAsync(request, deadline.Token);
            using var connection = await accepted;
            var response = await pending;

            // Assert
            response.ResponseStatus.Should().Be(ResponseStatus.TimedOut);
            deadline.IsCancellationRequested.Should().BeFalse();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CreateClient_SeparateWrappers_ReusesConnectionAndIsolatesCredentials(bool gzipResponse)
        {
            // Arrange
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var baseUrl = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            var served = ServeResponsesAsync(listener, new[]
            {
                CreateResponse("Set-Cookie: session=first; Path=/\r\n", false),
                CreateResponse("Connection: close\r\n", gzipResponse)
            }, deadline.Token);
            var firstFactory = new RequestFactory(new RequestOptions { Timeout = TimeSpan.FromSeconds(5) });
            var secondFactory = new RequestFactory(new RequestOptions { Timeout = TimeSpan.FromSeconds(8) });

            // Act
            RestResponse firstResponse;
            using (var first = firstFactory.CreateClient(baseUrl))
            {
                first.AddDefaultHeader("Authorization", "Bearer test-token");
                firstResponse = await first.ExecuteAsync(new RestRequest("/first"), deadline.Token);
            }
            using var second = secondFactory.CreateClient(baseUrl);
            var secondResponse = await second.ExecuteAsync(new RestRequest("/second"), deadline.Token);
            var requests = await served;

            // Assert
            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            secondResponse.Content.Should().Be("ok");
            second.Options.Timeout.Should().Be(TimeSpan.FromSeconds(8));
            requests.Should().HaveCount(2);
            requests[0].Should().StartWith("GET /first HTTP/1.1");
            requests[0].Should().Contain("Authorization: Bearer test-token");
            requests[1].Should().StartWith("GET /second HTTP/1.1");
            requests[1].Should().NotContain("Authorization:");
            requests[1].Should().NotContain("Cookie:");
            requests[0].Should().Contain("User-Agent: RestSharp/");
            requests[1].Should().Contain("User-Agent: RestSharp/");
        }

        [Test]
        public void CreateClient_WithNullTimeout_PreservesDefault()
        {
            // Arrange
            var factory = new RequestFactory(new RequestOptions());
            using var defaultClient = _factory.CreateClient("https://localhost");

            // Act
            using var client = factory.CreateClient("https://localhost");

            // Assert
            client.Should().NotBeNull();
            client.Options.Timeout.Should().Be(defaultClient.Options.Timeout);
        }

        [Test]
        public void CreateClient_WithoutOptions_DoesNotSetTimeout()
        {
            // Arrange
            using var defaultClient = _factory.CreateClient("https://localhost");

            // Act
            using var client = _factory.CreateClient("https://localhost");

            // Assert
            client.Should().NotBeNull();
            client.Options.Timeout.Should().Be(defaultClient.Options.Timeout);
        }

        [Test]
        public void CreateClient_WithTimeout_AppliesTimeout()
        {
            // Arrange
            var timeout = TimeSpan.FromSeconds(5);
            var factory = new RequestFactory(new RequestOptions { Timeout = timeout });

            // Act
            using var client = factory.CreateClient("https://localhost");

            // Assert
            client.Should().NotBeNull();
            client.Options.Timeout.Should().Be(timeout);
        }

        [Test]
        public void CreateRequest_CalledTwice_ReturnsNewInstances()
        {
            // Arrange
            // The factory is initialized in SetUp.

            // Act
            var request1 = _factory.CreateRequest("/resource");
            var request2 = _factory.CreateRequest("/resource");

            // Assert
            request1.Should().NotBeNull();
            request2.Should().NotBeNull();
            request1.Should().NotBeSameAs(request2);
        }

        private static byte[] CreateResponse(string headers, bool gzip)
        {
            var body = Encoding.UTF8.GetBytes("ok");
            if (gzip)
            {
                using var output = new MemoryStream();
                using (var compressor = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
                    compressor.Write(body, 0, body.Length);
                body = output.ToArray();
                headers += "Content-Encoding: gzip\r\n";
            }
            var prefix = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: " + body.Length + "\r\n" + headers + "\r\n");
            var response = new byte[prefix.Length + body.Length];
            prefix.CopyTo(response, 0);
            body.CopyTo(response, prefix.Length);
            return response;
        }

        private static async Task<string[]> ServeResponsesAsync(TcpListener listener, byte[][] responses, CancellationToken cancellationToken)
        {
            // Accept exactly one connection: both requests must reuse it, even after wrapper disposal.
            using var connection = await listener.AcceptTcpClientAsync(cancellationToken);
            using var stream = connection.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
            var requests = new List<string>();
            foreach (var response in responses)
            {
                var lines = new List<string>();
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != "")
                {
                    if (line is null)
                        throw new IOException("The shared HTTP connection closed before the next request.");
                    lines.Add(line);
                }
                var contentLength = 0;
                foreach (var header in lines)
                {
                    if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        contentLength = Int32.Parse(header.Substring("Content-Length:".Length).Trim());
                }
                var body = new char[contentLength];
                if (contentLength > 0 && await reader.ReadBlockAsync(body.AsMemory(), cancellationToken) != contentLength)
                    throw new EndOfStreamException("The request body ended before Content-Length.");
                requests.Add(String.Join("\r\n", lines) + "\r\n\r\n" + new string(body));
                await stream.WriteAsync(response.AsMemory(), cancellationToken);
            }
            return requests.ToArray();
        }
    }
}
