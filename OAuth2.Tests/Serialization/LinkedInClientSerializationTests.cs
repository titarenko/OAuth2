using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Tests.Serialization
{
    [TestFixture]
    public class LinkedInClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableLinkedInClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableLinkedInClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidXml_ReturnsCorrectFields()
        {
            // arrange
            const string content = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<person>
    <id>li-123</id>
    <first-name>Jane</first-name>
    <last-name>Doe</last-name>
    <email-address>jane@linkedin.com</email-address>
    <picture-url>https://linkedin.com/photo_80x80.jpg</picture-url>
</person>";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("li-123");
            info.FirstName.Should().Be("Jane");
            info.LastName.Should().Be("Doe");
            info.Email.Should().Be("jane@linkedin.com");
        }

        [Test]
        public void ParseUserInfo_WithPictureUrl_SetsNormalAvatarToOriginalUrl()
        {
            // arrange
            const string content = @"<?xml version=""1.0"" encoding=""UTF-8""?><person><id>1</id><first-name>A</first-name><last-name>B</last-name><picture-url>https://linkedin.com/photo_80x80.jpg</picture-url></person>";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Be("https://linkedin.com/photo_80x80.jpg");
        }

        [Test]
        public void ParseUserInfo_WithPictureUrl_FormatsSmallAvatarWithUnderscoreSize()
        {
            // arrange
            const string content = @"<?xml version=""1.0"" encoding=""UTF-8""?><person><id>1</id><first-name>A</first-name><last-name>B</last-name><picture-url>https://linkedin.com/photo_80_80.jpg</picture-url></person>";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Contain("36_36");
        }

        [Test]
        public void ParseUserInfo_MissingPictureUrl_UsesDefaultGhostAvatar()
        {
            // arrange
            const string content = @"<?xml version=""1.0"" encoding=""UTF-8""?><person><id>1</id><first-name>A</first-name><last-name>B</last-name></person>";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Contain("linkedin.com");
            info.AvatarUri.Normal.Should().Contain("ghost");
        }

        [Test]
        public void ParseUserInfo_ValidXml_SerializesToValidJson()
        {
            // arrange
            const string content = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<person>
    <id>li-123</id>
    <first-name>Jane</first-name>
    <last-name>Doe</last-name>
    <email-address>jane@linkedin.com</email-address>
    <picture-url>https://linkedin.com/photo_80x80.jpg</picture-url>
</person>";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("li-123");
            doc.RootElement.GetProperty("AvatarUri").ValueKind.Should().Be(JsonValueKind.Object);
        }

        private class TestableLinkedInClient : LinkedInClient
        {
            public TestableLinkedInClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
