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
    public class SpotifyClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableSpotifyClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableSpotifyClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""sp-1"",""display_name"":""DJ Cool"",""email"":""dj@spotify.com"",""images"":[{""url"":""https://spotify.com/img.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("sp-1");
            info.FirstName.Should().Be("DJ Cool");
            info.Email.Should().Be("dj@spotify.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsProviderName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""A"",""email"":""a@s.com"",""images"":[{""url"":""pic.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.ProviderName.Should().Be("Spotify");
        }

        [Test]
        public void ParseUserInfo_WithImages_SetsAllAvatarSizesToSameUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""A"",""email"":""a@s.com"",""images"":[{""url"":""https://spotify.com/img.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://spotify.com/img.jpg");
            info.AvatarUri.Normal.Should().Be("https://spotify.com/img.jpg");
            info.AvatarUri.Large.Should().Be("https://spotify.com/img.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingImages_AvatarUriIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""Test"",""email"":""t@s.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_NumericId_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":99999,""display_name"":""A"",""email"":""a@s.com"",""images"":[{""url"":""pic.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("99999");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""sp-1"",""display_name"":""DJ Cool"",""email"":""dj@spotify.com"",""images"":[{""url"":""https://spotify.com/img.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("ProviderName").GetString().Should().Be("Spotify");
            doc.RootElement.GetProperty("Id").GetString().Should().Be("sp-1");
        }

        private class TestableSpotifyClient : SpotifyClient
        {
            public TestableSpotifyClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
