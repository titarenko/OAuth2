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
    public class ExactOnlineClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableExactOnlineClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableExactOnlineClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectId()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""exact-1"",""display_name"":""Alice"",""email"":""alice@exact.com"",""images"":[{""url"":""https://exact.com/avatar.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("exact-1");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsFirstNameFromDisplayName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""Alice"",""email"":""a@e.com"",""images"":[{""url"":""pic.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Alice");
        }

        [Test]
        public void ParseUserInfo_WithImages_SetsAllAvatarSizesToSameUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""A"",""email"":""a@e.com"",""images"":[{""url"":""https://exact.com/avatar.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://exact.com/avatar.jpg");
            info.AvatarUri.Normal.Should().Be("https://exact.com/avatar.jpg");
            info.AvatarUri.Large.Should().Be("https://exact.com/avatar.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingImages_AvatarUriIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""Bob"",""email"":""b@e.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsProviderName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""A"",""email"":""a@e.com"",""images"":[{""url"":""pic.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.ProviderName.Should().Be("ExactOnline");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""exact-1"",""display_name"":""Alice"",""email"":""alice@exact.com"",""images"":[{""url"":""https://exact.com/avatar.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("exact-1");
            doc.RootElement.GetProperty("AvatarUri").ValueKind.Should().Be(JsonValueKind.Object);
        }

        private class TestableExactOnlineClient : ExactOnlineClient
        {
            public TestableExactOnlineClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
