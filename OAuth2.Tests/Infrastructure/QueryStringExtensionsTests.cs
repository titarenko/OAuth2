using NUnit.Framework;
using OAuth2.Infrastructure;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class QueryStringExtensionsTests
    {
        [Test]
        public void Should_ReturnDictionary_WhenCorrectQueryStringIsPassed()
        {
            // arrange
            var line = "par1=val1&par2=val2";

            // act
            var dictionary = line.ToDictionary();

            // assert
            dictionary.Should().ContainKey("par1");
            dictionary.Should().ContainKey("par2");

            dictionary["par1"].Should().Be("val1");
            dictionary["par2"].Should().Be("val2");
        }

        [Test]
        public void Should_ReturnQueryString_WithSnakeCasedKeysAndStringifiedValuesTakenFromProperties()
        {
            // arrange
            var instance = new
            {
                ClientId = 10,
                State = "ready"
            };

            // act
            var line = instance.ToQueryString();

            // assert
            line.Should().Be("client_id=10&state=ready");
        }
    }
}