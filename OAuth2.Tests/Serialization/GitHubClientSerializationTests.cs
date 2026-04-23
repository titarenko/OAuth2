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
    public class GitHubClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableGitHubClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableGitHubClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":123,""login"":""octocat"",""name"":""Octo Cat"",""email"":""octo@github.com"",""avatar_url"":""https://github.com/avatar.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("123");
            info.FirstName.Should().Be("Octo");
            info.LastName.Should().Be("Cat");
            info.Email.Should().Be("octo@github.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsProviderNameToGitHub()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""login"":""u"",""name"":""A B"",""email"":null,""avatar_url"":""a.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.ProviderName.Should().Be("GitHub");
        }

        [Test]
        public void ParseUserInfo_NullName_FallsBackToLogin()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""login"":""bot"",""name"":null,""email"":null,""avatar_url"":""a.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("bot");
            info.LastName.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_ThreeWordName_FirstIsFirstNameLastIsLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""login"":""user"",""name"":""Mary Jane Watson"",""email"":null,""avatar_url"":""a.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Mary");
            info.LastName.Should().Be("Watson");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsSmallAvatarWith36()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""login"":""u"",""name"":""A B"",""email"":null,""avatar_url"":""https://github.com/avatar.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://github.com/avatar.jpg&s=36");
        }

        [Test]
        public void ParseUserInfo_ValidContent_NormalAvatarIsUnmodifiedUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""login"":""u"",""name"":""A B"",""email"":null,""avatar_url"":""https://github.com/avatar.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Be("https://github.com/avatar.jpg");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsLargeAvatarWith300()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":1,""login"":""u"",""name"":""A B"",""email"":null,""avatar_url"":""https://github.com/avatar.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Large.Should().Be("https://github.com/avatar.jpg&s=300");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":123,""login"":""octocat"",""name"":""Octo Cat"",""email"":""octo@github.com"",""avatar_url"":""https://github.com/avatar.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("ProviderName").GetString().Should().Be("GitHub");
            doc.RootElement.GetProperty("Id").GetString().Should().Be("123");
        }

        private class TestableGitHubClient : GitHubClient
        {
            public TestableGitHubClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
