using NUnit.Framework;
using OAuth2.Parameters;
using FluentAssertions;

namespace OAuth2.Tests.Parameters
{
    [TestFixture]
    public class AccessCodeRequestParametersTests
    {
        [Test]
        public void Should_HaveResponseTypePredefined()
        {
            new AccessCodeRequestParameters().ResponseType.Should().Be("code");
        }
    }
}