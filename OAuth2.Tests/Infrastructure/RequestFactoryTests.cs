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
        public void Should_ReturnNewClientInstance_WhenNewClientIsCalled()
        {
            // act
            var client1 = _factory.CreateClient();
            var client2 = _factory.CreateClient();

            // assert
            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client1.Should().NotBeSameAs(client2);
        }

        [Test]
        public void Should_ReturnNewRequestInstance_WhenNewRequestIsCalled()
        {
            // act
            var request1 = _factory.CreateRequest();
            var request2 = _factory.CreateRequest();

            // assert
            request1.Should().NotBeNull();
            request2.Should().NotBeNull();
            request1.Should().NotBeSameAs(request2);
        }
    }
}