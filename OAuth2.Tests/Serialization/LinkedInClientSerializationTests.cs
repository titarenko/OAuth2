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
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableLinkedInClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableLinkedInClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidJson_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""sub"":""li-123"",""given_name"":""Jane"",""family_name"":""Doe"",""email"":""jane@linkedin.com"",""picture"":""https://media.licdn.com/photo.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("li-123");
            info.FirstName.Should().Be("Jane");
            info.LastName.Should().Be("Doe");
            info.Email.Should().Be("jane@linkedin.com");
        }

        [Test]
        public void ParseUserInfo_WithPicture_SetsAllAvatarSizesToPictureUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""sub"":""1"",""given_name"":""A"",""family_name"":""B"",""picture"":""https://media.licdn.com/photo.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://media.licdn.com/photo.jpg");
            info.AvatarUri.Normal.Should().Be("https://media.licdn.com/photo.jpg");
            info.AvatarUri.Large.Should().Be("https://media.licdn.com/photo.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingPicture_AvatarFieldsAreNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""sub"":""1"",""given_name"":""A"",""family_name"":""B""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""sub"":""1"",""given_name"":""A"",""family_name"":""B""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidJson_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""sub"":""li-123"",""given_name"":""Jane"",""family_name"":""Doe"",""email"":""jane@linkedin.com"",""picture"":""https://media.licdn.com/photo.jpg""}";

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
