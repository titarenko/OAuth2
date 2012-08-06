using NUnit.Framework;
using OAuth2.Configuration;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class ConfigurationManagerTests
    {
        private ConfigurationManager manager;

        [SetUp]
        public void SetUp()
        {
            manager = new ConfigurationManager();
        }

        [Test]
        public void Should_ReturnValue_WhenItIsSetInAppConfig()
        {
            // act
            var setting = manager.GetAppSetting("Key");

            // assert
            setting.Should().Be("Value");
        }

        [Test]
        public void Should_ReturnNull_WhenSettingWithRequestedKeyIsNotFound()
        {
            // act
            var setting = manager.GetAppSetting("Value");

            // assert
            setting.Should().BeNull();
        }

        [Test]
        public void Should_ReturnConfigurationSection_WhenItIsInConfig()
        {
            // act
            var section = manager.GetConfigSection<OAuth2ConfigurationSection>("oauth2");

            // assert
            section.Should().NotBeNull();
        }

        [Test]
        public void Should_ReturnNull_WhenRequestedConfigurationSectionIsNotFound()
        {
            // act
            var section = manager.GetConfigSection<OAuth2ConfigurationSection>("notfound");

            // assert
            section.Should().BeNull();
        }
    }
}