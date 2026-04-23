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
    public class OdnoklassnikiClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableOdnoklassnikiClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableOdnoklassnikiClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":""ok-1"",""first_name"":""Olga"",""last_name"":""Ivanova"",""pic_1"":""https://ok.ru/pic.jpg?id=123&photoType=4""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("ok-1");
            info.FirstName.Should().Be("Olga");
            info.LastName.Should().Be("Ivanova");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsNormalAvatarFromPic1()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":""1"",""first_name"":""A"",""last_name"":""B"",""pic_1"":""https://ok.ru/pic.jpg?id=1&photoType=4""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Contain("photoType=4");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReplacesPhotoType4With6ForLargeAvatar()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":""1"",""first_name"":""A"",""last_name"":""B"",""pic_1"":""https://ok.ru/pic.jpg?id=1&photoType=4""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Large.Should().Contain("photoType=6");
            info.AvatarUri.Large.Should().NotContain("photoType=4");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SmallAvatarIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":""1"",""first_name"":""A"",""last_name"":""B"",""pic_1"":""https://ok.ru/pic.jpg?id=1&photoType=4""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_NumericUid_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":12345,""first_name"":""A"",""last_name"":""B"",""pic_1"":""https://ok.ru/pic.jpg?id=1&photoType=4""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("12345");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":""ok-1"",""first_name"":""Olga"",""last_name"":""Ivanova"",""pic_1"":""https://ok.ru/pic.jpg?id=123&photoType=4""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("ok-1");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Large").GetString()
                .Should().Contain("photoType=6");
        }

        private class TestableOdnoklassnikiClient : OdnoklassnikiClient
        {
            public TestableOdnoklassnikiClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
