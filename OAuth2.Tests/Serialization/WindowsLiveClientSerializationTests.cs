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
    public class WindowsLiveClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableWindowsLiveClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableWindowsLiveClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            _configuration.Scope.Returns("WL.EMAILS");
            /* lang=json */
            const string content = @"{""id"":""wl-123"",""first_name"":""Win"",""last_name"":""User"",""emails"":{""preferred"":""win@live.com""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("wl-123");
            info.FirstName.Should().Be("Win");
            info.LastName.Should().Be("User");
            info.Email.Should().Be("win@live.com");
        }

        [Test]
        public void ParseUserInfo_ScopeIncludesEmails_ParsesEmail()
        {
            // arrange
            _configuration.Scope.Returns("WL.EMAILS");
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""emails"":{""preferred"":""a@live.com""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().Be("a@live.com");
        }

        [Test]
        public void ParseUserInfo_ScopeDoesNotIncludeEmails_EmailIsNull()
        {
            // arrange
            _configuration.Scope.Returns("WL.BASIC");
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""emails"":{""preferred"":""a@live.com""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsAvatarUrlsWithId()
        {
            // arrange
            _configuration.Scope.Returns("WL.BASIC");
            /* lang=json */
            const string content = @"{""id"":""wl-123"",""first_name"":""A"",""last_name"":""B"",""emails"":{}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Contain("wl-123");
            info.AvatarUri.Normal.Should().Contain("wl-123");
            info.AvatarUri.Large.Should().Contain("wl-123");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SmallAndNormalAvatarBothContainUserTileSmall()
        {
            // arrange
            _configuration.Scope.Returns("WL.BASIC");
            /* lang=json */
            const string content = @"{""id"":""wl-123"",""first_name"":""A"",""last_name"":""B"",""emails"":{}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Contain("UserTileSmall");
            info.AvatarUri.Normal.Should().Contain("UserTileSmall");
        }

        [Test]
        public void ParseUserInfo_LowercaseScope_StillParsesEmail()
        {
            // arrange
            _configuration.Scope.Returns("wl.emails");
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""emails"":{""preferred"":""a@live.com""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().Be("a@live.com");
        }

        [Test]
        public void ParseUserInfo_NullScope_EmailIsNull()
        {
            // arrange
            _configuration.Scope.Returns((string)null);
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""emails"":{""preferred"":""a@live.com""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_LargeAvatarContainsUserTileLarge()
        {
            // arrange
            _configuration.Scope.Returns("WL.BASIC");
            /* lang=json */
            const string content = @"{""id"":""wl-123"",""first_name"":""A"",""last_name"":""B"",""emails"":{}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Large.Should().Contain("UserTileLarge");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            _configuration.Scope.Returns("WL.EMAILS");
            /* lang=json */
            const string content = @"{""id"":""wl-123"",""first_name"":""Win"",""last_name"":""User"",""emails"":{""preferred"":""win@live.com""}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("wl-123");
            doc.RootElement.GetProperty("Email").GetString().Should().Be("win@live.com");
        }

        private class TestableWindowsLiveClient : WindowsLiveClient
        {
            public TestableWindowsLiveClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
