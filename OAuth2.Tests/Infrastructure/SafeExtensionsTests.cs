using System;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Infrastructure;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class SafeExtensionsTests
    {
        [Test]
        public void Should_NotThrow_WhenSafeGetIsCalledOnNull()
        {
            // arrange
            IClient client = null;

            // act & assert
            client.Invoking(x => x.SafeGet(z => z.GetAccessCodeRequestUri()))
                .ShouldNotThrow<NullReferenceException>();
            client.SafeGet(x => x.GetAccessCodeRequestUri()).Should().Be(null);
        }

        [Test]
        public void Should_ReturnResultObtainedFromSelector_WhenSafeGetIsCalledOnInstance()
        {
            // arrange
            var value = "abc";
            Func<string, string> selector = x => x.Substring(1);

            // act & assert
            value.SafeGet(selector).Should().Be(selector(value));
        }
    }
}