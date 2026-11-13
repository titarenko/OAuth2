using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OAuth2.Tests.TestHelpers
{
    /// <summary>
    /// A mock HttpMessageHandler that queues responses and tracks sent requests.
    /// Used for testing with RestSharp 114+ which requires concrete RestClient instances.
    /// </summary>
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode StatusCode, string Content)> _responses = new();

        public List<HttpRequestMessage> SentRequests { get; } = new();
        public List<string?> SentBodies { get; } = new();

        public void EnqueueResponse(HttpStatusCode statusCode, string content)
        {
            _responses.Enqueue((statusCode, content));
        }

        public void EnqueueResponse(string content)
        {
            _responses.Enqueue((HttpStatusCode.OK, content));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SentRequests.Add(request);
            SentBodies.Add(request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException(
                    "No queued mock HTTP response is available for the incoming request. " +
                    "Ensure EnqueueResponse is called for each expected request.");
            }

            var (statusCode, content) = _responses.Dequeue();

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var request in SentRequests)
                    request.Dispose();

                SentRequests.Clear();
                SentBodies.Clear();
                _responses.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
