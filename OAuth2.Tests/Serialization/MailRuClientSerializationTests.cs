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
    public class MailRuClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableMailRuClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableMailRuClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_JsonArray_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"[{""uid"":""mail-1"",""first_name"":""Ivan"",""last_name"":""Petrov"",""email"":""ivan@mail.ru"",""pic"":""https://mail.ru/pic.jpg""}]";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("mail-1");
            info.FirstName.Should().Be("Ivan");
            info.LastName.Should().Be("Petrov");
            info.Email.Should().Be("ivan@mail.ru");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsOnlyNormalAvatar()
        {
            // arrange
            /* lang=json */
            const string content = @"[{""uid"":""1"",""first_name"":""A"",""last_name"":""B"",""email"":""a@m.ru"",""pic"":""https://mail.ru/pic.jpg""}]";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().Be("https://mail.ru/pic.jpg");
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"[{""uid"":""1"",""first_name"":""A"",""last_name"":""B"",""pic"":""p.jpg""}]";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_NumericUid_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"[{""uid"":99999,""first_name"":""A"",""last_name"":""B"",""email"":""a@m.ru"",""pic"":""p.jpg""}]";

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
            const string content = @"[{""uid"":""mail-1"",""first_name"":""Ivan"",""last_name"":""Petrov"",""email"":""ivan@mail.ru"",""pic"":""https://mail.ru/pic.jpg""}]";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("mail-1");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString()
                .Should().Be("https://mail.ru/pic.jpg");
        }

        private class TestableMailRuClient : MailRuClient
        {
            public TestableMailRuClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
