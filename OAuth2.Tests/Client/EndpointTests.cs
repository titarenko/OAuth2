using NUnit.Framework;
using OAuth2.Client;
using FluentAssertions;

namespace OAuth2.Tests.Client
{
    [TestFixture]
    public class EndpointTests
    {
        [Test]
        [TestCase("https://base.com", "/resource", "https://base.com/resource")]
        public void Uri_BaseUriAndResource_ReturnsCombinedUri(
            string baseUri, string resource, string uri)
        {
            // arrange
            var endpoint = new Endpoint
            {
                BaseUri = baseUri,
                Resource = resource
            };

            // act & assert
            endpoint.Uri.Should().Be(uri);
        }
    }
}