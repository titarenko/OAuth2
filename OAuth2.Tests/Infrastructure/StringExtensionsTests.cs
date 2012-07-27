using FluentAssertions;
using NUnit.Framework;
using OAuth2.Infrastructure;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [Test]
        [TestCase("code", "Code")]
        [TestCase("client_id", "ClientId")]
        [TestCase("client_secret", "ClientSecret")]
        public void Should_ReturnResultForInput_WhenFromCamelToSnakeCaseIsCalled(string result, string input)
        {
            input.FromCamelToSnakeCase().Should().Be(result);
        }
    }
}