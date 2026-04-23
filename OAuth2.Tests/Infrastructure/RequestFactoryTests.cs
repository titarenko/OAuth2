using NUnit.Framework;
using OAuth2.Infrastructure;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class RequestFactoryTests
    {
        private RequestFactory _factory;

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
