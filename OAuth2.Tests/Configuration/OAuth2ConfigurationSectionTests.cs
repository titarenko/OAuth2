using FluentAssertions;
using NUnit.Framework;
using OAuth2.Configuration;

namespace OAuth2.Tests.Configuration
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
            section["SomeClient"].Should().NotBeNull();
            section["SomeAnotherClient"].Should().NotBeNull();
        }

        [Test]
        public void Should_CorrectlyParseServiceDefinition()
        {
            // act
            var section = configurationManager.GetConfigSection<OAuth2ConfigurationSection>("oauth2");
            var service = section["SomeAnotherClient"];

            // assert
            service.ClientTypeName.Should().Be("SomeAnotherClient");
            service.ClientId.Should().Be("SomeAnotherClientId");
            service.ClientSecret.Should().Be("SomeAnotherClientSecret");
            service.Scope.Should().Be("SomeAnotherScope");
            service.RedirectUri.Should().Be("https://some-another-redirect-uri.net");
        }
    }
}