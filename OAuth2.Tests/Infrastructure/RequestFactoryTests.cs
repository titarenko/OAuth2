using NUnit.Framework;
using OAuth2.Infrastructure;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class RequestFactoryTests
    {
        private RequestFactory factory;

        [SetUp]
        public void SetUp()
        {
            // arrange
            factory = new RequestFactory();
        }

        [Test]
        public void Should_ReturnNewClientInstance_WhenNewClientIsCalled()
        {
            // act
            var client1 = factory.NewClient();
            var client2 = factory.NewClient();

            // assert
            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client1.Should().NotBeSameAs(client2);
        }

        [Test]
        public void Should_ReturnNewRequestInstance_WhenNewRequestIsCalled()
        {
            // act
            var request1 = factory.NewRequest();
            var request2 = factory.NewRequest();

            // assert
            request1.Should().NotBeNull();
            request2.Should().NotBeNull();
            request1.Should().NotBeSameAs(request2);
        }
    }
}