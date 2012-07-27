using NUnit.Framework;
using OAuth2.Parameters;
using FluentAssertions;

namespace OAuth2.Tests.Parameters
{
    [TestFixture]
    public class AccessTokenRequestParametersTests
    {
        [Test]
        public void Should_HaveGrantTypePredefined()
        {
            new AccessTokenRequestParameters().GrantType.Should().Be("authorization_code");
        }
    }
}