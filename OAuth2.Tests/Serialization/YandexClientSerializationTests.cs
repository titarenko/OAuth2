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
    public class YandexClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableYandexClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableYandexClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""ya-1"",""real_name"":""Yuri Gagarin"",""display_name"":""ygagarin"",""default_email"":""yuri@yandex.ru"",""default_avatar_id"":""avatar123""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("ya-1");
            info.FirstName.Should().Be("Yuri");
            info.LastName.Should().Be("Gagarin");
            info.Email.Should().Be("yuri@yandex.ru");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsAvatarUrlsWithYapicBase()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""real_name"":""A B"",""display_name"":""ab"",""default_email"":null,""default_avatar_id"":""avatar123""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://avatars.yandex.net/get-yapic/avatar123/islands-middle");
            info.AvatarUri.Normal.Should().Be("https://avatars.yandex.net/get-yapic/avatar123/islands-retina-50");
            info.AvatarUri.Large.Should().Be("https://avatars.yandex.net/get-yapic/avatar123/islands-200");
        }

        [Test]
        public void ParseUserInfo_EmptyAvatarId_AvatarUriFieldsAreNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""real_name"":""A B"",""display_name"":""ab"",""default_email"":null,""default_avatar_id"":""""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_SingleWordRealName_LastNameIsEmpty()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""real_name"":""Madonna"",""display_name"":""md"",""default_email"":null,""default_avatar_id"":""""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Madonna");
            info.LastName.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""real_name"":""A B"",""display_name"":""ab"",""default_avatar_id"":""""}";

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
            const string content = @"{""id"":""ya-1"",""real_name"":""Yuri Gagarin"",""display_name"":""ygagarin"",""default_email"":""yuri@yandex.ru"",""default_avatar_id"":""avatar123""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("ya-1");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Small").GetString()
                .Should().Contain("avatar123");
        }

        private class TestableYandexClient : YandexClient
        {
            public TestableYandexClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
