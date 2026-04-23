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
    public class TwitterClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableTwitterClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableTwitterClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":987,""name"":""Tweet Bird"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("987");
            info.FirstName.Should().Be("Tweet");
            info.LastName.Should().Be("Bird");
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReplacesNormalWithMiniForSmallAvatar()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""name"":""A B"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://twitter.com/pic_mini.jpg");
        }

        [Test]
        public void ParseUserInfo_ValidContent_KeepsNormalAvatarUnchanged()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""name"":""A B"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Be("https://twitter.com/pic_normal.jpg");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReplacesNormalWithBiggerForLargeAvatar()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""name"":""A B"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Large.Should().Be("https://twitter.com/pic_bigger.jpg");
        }

        [Test]
        public void ParseUserInfo_SingleWordName_LastNameIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""name"":""Cher"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Cher");
            info.LastName.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ThreeWordName_LastNameContainsAllAfterFirstSpace()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""name"":""Mary Jane Watson"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Mary");
            info.LastName.Should().Be("Jane Watson");
        }

        [Test]
        public void ParseUserInfo_EmailAlwaysNull_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""name"":""A B"",""profile_image_url"":""pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":987,""name"":""Tweet Bird"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("987");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Small").GetString()
                .Should().Contain("mini");
        }

        private class TestableTwitterClient : TwitterClient
        {
            public TestableTwitterClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
