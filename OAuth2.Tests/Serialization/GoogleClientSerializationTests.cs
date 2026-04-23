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
    public class GoogleClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableGoogleClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableGoogleClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""g-123"",""email"":""user@gmail.com"",""given_name"":""John"",""family_name"":""Smith"",""picture"":""https://google.com/photo.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("g-123");
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Smith");
            info.Email.Should().Be("user@gmail.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsSmallAvatarWithSz36()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""e@g.com"",""given_name"":""A"",""family_name"":""B"",""picture"":""https://google.com/photo.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://google.com/photo.jpg?sz=36");
        }

        [Test]
        public void ParseUserInfo_ValidContent_NormalAvatarIsUnmodifiedUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""e@g.com"",""given_name"":""A"",""family_name"":""B"",""picture"":""https://google.com/photo.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Be("https://google.com/photo.jpg");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsLargeAvatarWithSz300()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""e@g.com"",""given_name"":""A"",""family_name"":""B"",""picture"":""https://google.com/photo.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Large.Should().Be("https://google.com/photo.jpg?sz=300");
        }

        [Test]
        public void ParseUserInfo_MissingPicture_AvatarSmallAndLargeAreEmpty()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""e@g.com"",""given_name"":""A"",""family_name"":""B""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeEmpty();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""g-123"",""email"":""user@gmail.com"",""given_name"":""John"",""family_name"":""Smith"",""picture"":""https://google.com/photo.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("g-123");
            doc.RootElement.GetProperty("Email").GetString().Should().Be("user@gmail.com");
        }

        private class TestableGoogleClient : GoogleClient
        {
            public TestableGoogleClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
