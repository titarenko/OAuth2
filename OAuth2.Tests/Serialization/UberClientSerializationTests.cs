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
    public class UberClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableUberClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableUberClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Uber"",""last_name"":""Rider"",""email"":""rider@uber.com"",""picture"":""https://uber.com/pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Uber");
            info.LastName.Should().Be("Rider");
            info.Email.Should().Be("rider@uber.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsAllAvatarSizesToSameUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""A"",""last_name"":""B"",""email"":""a@u.com"",""picture"":""https://uber.com/pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://uber.com/pic.jpg");
            info.AvatarUri.Normal.Should().Be("https://uber.com/pic.jpg");
            info.AvatarUri.Large.Should().Be("https://uber.com/pic.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingAllFields_ReturnsEmptyUserInfo()
        {
            // arrange
            /* lang=json */
            const string content = @"{}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().BeNull();
            info.LastName.Should().BeNull();
            info.Email.Should().BeNull();
            info.AvatarUri.Small.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_PartialFields_ReturnsOnlyPresentFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Test""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Test");
            info.LastName.Should().BeNull();
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_CaseInsensitiveProperties_MatchesKeys()
        {
            // arrange
            /* lang=json */
            const string content = @"{""First_Name"":""Uber"",""Last_Name"":""Rider"",""Email"":""rider@uber.com"",""Picture"":""https://uber.com/pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Uber");
            info.LastName.Should().Be("Rider");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Uber"",""last_name"":""Rider"",""email"":""rider@uber.com"",""picture"":""https://uber.com/pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("FirstName").GetString().Should().Be("Uber");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString()
                .Should().Be("https://uber.com/pic.jpg");
        }

        private class TestableUberClient : UberClient
        {
            public TestableUberClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
