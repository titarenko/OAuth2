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
    public class YahooClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableYahooClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableYahooClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectNames()
        {
            // arrange
            /* lang=json */
            const string content = @"{""profile"":{""givenName"":""John"",""familyName"":""Yahoo"",""image"":{""imageUrl"":""https://yahoo.com/pic.jpg""}},""emails"":""john@yahoo.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Yahoo");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsAllAvatarSizesToSameUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""profile"":{""givenName"":""A"",""familyName"":""B"",""image"":{""imageUrl"":""https://yahoo.com/pic.jpg""}},""emails"":""a@y.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://yahoo.com/pic.jpg");
            info.AvatarUri.Normal.Should().Be("https://yahoo.com/pic.jpg");
            info.AvatarUri.Large.Should().Be("https://yahoo.com/pic.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingImage_AvatarUriIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""profile"":{""givenName"":""A"",""familyName"":""B""},""emails"":""a@y.com""}";

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
            const string content = @"{""profile"":{""givenName"":""A"",""familyName"":""B"",""image"":{""imageUrl"":""pic.jpg""}},""emails"":""a@y.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.ProviderName.Should().Be("Yahoo");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""profile"":{""givenName"":""John"",""familyName"":""Yahoo"",""image"":{""imageUrl"":""https://yahoo.com/pic.jpg""}},""emails"":""john@yahoo.com""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("ProviderName").GetString().Should().Be("Yahoo");
            doc.RootElement.GetProperty("AvatarUri").ValueKind.Should().Be(JsonValueKind.Object);
        }

        private class TestableYahooClient : YahooClient
        {
            public TestableYahooClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
