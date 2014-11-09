﻿using NUnit.Framework;
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
            var client1 = factory.CreateClient();
            var client2 = factory.CreateClient();

            // assert
            client1.Should().NotBeNull();
            client2.Should().NotBeNull();
            client1.Should().NotBeSameAs(client2);
        }

        [Test]
        public void Should_ReturnNewRequestInstance_WhenNewRequestIsCalled()
        {
            // act
            var request1 = factory.CreateRequest(null);
            var request2 = factory.CreateRequest(null);

            // assert
            request1.Should().NotBeNull();
            request2.Should().NotBeNull();
            request1.Should().NotBeSameAs(request2);
        }
    }
}