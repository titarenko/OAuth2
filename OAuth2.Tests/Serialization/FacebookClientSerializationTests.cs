using System;
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
    public class FacebookClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableFacebookClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableFacebookClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""fb-123"",""first_name"":""Mark"",""last_name"":""User"",""email"":""mark@fb.com"",""picture"":{""data"":{""url"":""https://fb.com/pic.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("fb-123");
            info.FirstName.Should().Be("Mark");
            info.LastName.Should().Be("User");
            info.Email.Should().Be("mark@fb.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsAvatarUrlsWithSizeType()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""email"":null,""picture"":{""data"":{""url"":""https://fb.com/pic.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://fb.com/pic.jpg?type=small");
            info.AvatarUri.Normal.Should().Be("https://fb.com/pic.jpg?type=normal");
            info.AvatarUri.Large.Should().Be("https://fb.com/pic.jpg?type=large");
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""picture"":{""data"":{""url"":""pic.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_NumericId_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":999,""first_name"":""A"",""last_name"":""B"",""picture"":{""data"":{""url"":""pic.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("999");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""fb-123"",""first_name"":""Mark"",""last_name"":""User"",""email"":""mark@fb.com"",""picture"":{""data"":{""url"":""https://fb.com/pic.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("fb-123");
            doc.RootElement.GetProperty("FirstName").GetString().Should().Be("Mark");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Small").GetString().Should().Contain("small");
        }

        private class TestableFacebookClient : FacebookClient
        {
            public TestableFacebookClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
