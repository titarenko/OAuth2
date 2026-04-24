using System;
using FluentAssertions;
using NUnit.Framework;
using OAuth2.Infrastructure;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class RequestFactoryTests
    {
        private RequestFactory _factory = null!;

        [SetUp]
        public void SetUp()
        {
            // arrange
            _factory = new RequestFactory();
        }

        [Test]
        public void CreateClient_CalledTwice_ReturnsNewInstances()
        {
            // act
            var client1 = _factory.CreateClient("https://localhost");
            var client2 = _factory.CreateClient("https://localhost");

            // assert
            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client1.Should().NotBeSameAs(client2);
        }

        [Test]
        public void CreateClient_WithTimeout_AppliesTimeout()
        {
            // arrange
            var timeout = TimeSpan.FromSeconds(5);
            var options = new RequestOptions { Timeout = timeout };

            // act
            var client = _factory.CreateClient("https://localhost", options);

            // assert
            client.Should().NotBeNull();
            client.Options.Timeout.Should().Be(timeout);
        }

        [Test]
        public void CreateClient_WithNullTimeout_PreservesDefault()
        {
            // arrange
            var options = new RequestOptions();
            var defaultClient = _factory.CreateClient("https://localhost");

            // act
            var client = _factory.CreateClient("https://localhost", options);

            // assert
            client.Should().NotBeNull();
            client.Options.Timeout.Should().Be(defaultClient.Options.Timeout);
        }

        [Test]
        public void CreateClient_WithNullOptions_PreservesDefault()
        {
            // arrange
            var defaultClient = _factory.CreateClient("https://localhost");

            // act
            var client = _factory.CreateClient("https://localhost", (RequestOptions?)null);

            // assert
            client.Should().NotBeNull();
            client.Options.Timeout.Should().Be(defaultClient.Options.Timeout);
        }

        [Test]
        public void CreateRequest_CalledTwice_ReturnsNewInstances()
        {
            // act
            var request1 = _factory.CreateRequest("/resource");
            var request2 = _factory.CreateRequest("/resource");

            // assert
            request1.Should().NotBeNull();
            request2.Should().NotBeNull();
            request1.Should().NotBeSameAs(request2);
        }
    }
}
