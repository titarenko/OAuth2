using NUnit.Framework;
using OAuth2.Infrastructure;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class OAuth2ConfigurationSectionTests
    {
        private ConfigurationManager configurationManager;

        [SetUp]
        public void SetUp()
        {
            configurationManager = new ConfigurationManager();
        }

        [Test]
        public void Should_ContainAllServiceDefinitions()
        {
            // act
            var section = configurationManager.GetConfigSection<OAuth2ConfigurationSection>("oauth2");

            // assert
            section.Services.Count.Should().Be(2);
        }

        [Test]
        public void Should_CorrectlyParseServiceDefinition()
        {
            // act
            var section = configurationManager.GetConfigSection<OAuth2ConfigurationSection>("oauth2");
            var service = section.Services["SomeAnotherClient"];

            // assert
            service.ClientTypeName.Should().Be("SomeAnotherClient");
            service.ClientId.Should().Be("SomeAnotherClientId");
            service.ClientSecret.Should().Be("SomeAnotherClientSecret");
            service.Scope.Should().Be("SomeAnotherScope");
            service.RedirectUri.Should().Be("https://some-another-redirect-uri.net");
        }
    }
}