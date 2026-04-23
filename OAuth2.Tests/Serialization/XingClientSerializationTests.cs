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
    public class XingClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableXingClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableXingClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""users"":[{""id"":""xing-1"",""first_name"":""Max"",""last_name"":""Mustermann"",""active_email"":""max@xing.com"",""photo_urls"":{""size_48x48"":""https://xing.com/48.jpg"",""size_128x128"":""https://xing.com/128.jpg"",""size_256x256"":""https://xing.com/256.jpg""}}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("xing-1");
            info.FirstName.Should().Be("Max");
            info.LastName.Should().Be("Mustermann");
            info.Email.Should().Be("max@xing.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_MapsPhotoUrlsToAvatarSizes()
        {
            // arrange
            /* lang=json */
            const string content = @"{""users"":[{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""active_email"":""a@x.com"",""photo_urls"":{""size_48x48"":""https://xing.com/48.jpg"",""size_128x128"":""https://xing.com/128.jpg"",""size_256x256"":""https://xing.com/256.jpg""}}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://xing.com/48.jpg");
            info.AvatarUri.Normal.Should().Be("https://xing.com/128.jpg");
            info.AvatarUri.Large.Should().Be("https://xing.com/256.jpg");
        }

        [Test]
        public void ParseUserInfo_EmptyUsersArray_ReturnsEmptyUserInfo()
        {
            // arrange
            /* lang=json */
            const string content = @"{""users"":[]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().BeNull();
            info.FirstName.Should().BeNull();
            info.LastName.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_NullPhotoUrls_AvatarUriIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""users"":[{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""active_email"":""a@b.com"",""photo_urls"":null}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""users"":[{""id"":""xing-1"",""first_name"":""Max"",""last_name"":""Mustermann"",""active_email"":""max@xing.com"",""photo_urls"":{""size_48x48"":""https://xing.com/48.jpg"",""size_128x128"":""https://xing.com/128.jpg"",""size_256x256"":""https://xing.com/256.jpg""}}]}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("xing-1");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Small").GetString()
                .Should().Be("https://xing.com/48.jpg");
        }

        private class TestableXingClient : XingClient
        {
            public TestableXingClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
