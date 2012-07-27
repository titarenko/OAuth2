using NSubstitute;
using NUnit.Framework;
using OAuth2.Infrastructure;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class AppConfigTests
    {
        [Test]
        public void Should_HandleSubsectionsAsPrefixedKeys()
        {
            // arrange
            var manager = Substitute.For<IConfigurationManager>();
            manager.GetAppSetting(Arg.Is("section.key")).Returns("value");
            var config = new AppConfig(manager);
            
            // act
            var value = config.GetSection("section").Get("key");

            // assert
            value.Should().Be("value");
        }

        [Test]
        public void Should_FillObjectPropertiesUsingAppSettings()
        {
            // arrange
            var manager = Substitute.For<IConfigurationManager>();
            manager.GetAppSetting(Arg.Is("ClientId")).Returns("Id");
            manager.GetAppSetting(Arg.Is("Code")).Returns("1234");

            var config = new AppConfig(manager);

            // act
            var parameters = config.Get<Parameters>();

            // assert
            parameters.ClientId.Should().Be("Id");
            parameters.Code.Should().Be(1234);
        }

        [Test]
        public void Should_ConvertValueToTargetType()
        {
            // arrange
            var manager = Substitute.For<IConfigurationManager>();
            manager.GetAppSetting(Arg.Is("key")).Returns("123");
            var config = new AppConfig(manager);

            // act
            var value = config.Get<int>("key");

            // assert
            value.Should().Be(123);
        }

        [Test]
        public void Should_ReturnSectionByTypeName()
        {
            // arrange
            var manager = Substitute.For<IConfigurationManager>();
            manager.GetAppSetting(Arg.Is("Parameters.key")).Returns("753");
            var config = new AppConfig(manager);

            // act
            var value = config.GetSection<Parameters>().Get("key");

            // assert
            value.Should().Be("753");
        }

        class Parameters
        {
            public string ClientId { get; set; }

            public int Code { get; set; }
        }
    }
}