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
    public class TodoistClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableTodoistClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableTodoistClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1234"",""email"":""user@todoist.com"",""full_name"":""Todo User"",""avatar_small"":""https://td.com/s.jpg"",""avatar_medium"":""https://td.com/m.jpg"",""avatar_big"":""https://td.com/l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("1234");
            info.Email.Should().Be("user@todoist.com");
            info.LastName.Should().Be("Todo User");
        }

        [Test]
        public void ParseUserInfo_ValidContent_MapsAvatarSizesToCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""a@t.com"",""full_name"":""A"",""avatar_small"":""https://td.com/s.jpg"",""avatar_medium"":""https://td.com/m.jpg"",""avatar_big"":""https://td.com/l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://td.com/s.jpg");
            info.AvatarUri.Normal.Should().Be("https://td.com/m.jpg");
            info.AvatarUri.Large.Should().Be("https://td.com/l.jpg");
        }

        [Test]
        public void ParseUserInfo_StringId_ReturnsIdAsIs()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""99999"",""email"":""a@t.com"",""full_name"":""A"",""avatar_small"":""s.jpg"",""avatar_medium"":""m.jpg"",""avatar_big"":""l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("99999");
        }

        [Test]
        public void ParseUserInfo_NumericId_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":99999,""email"":""a@t.com"",""full_name"":""A"",""avatar_small"":""s.jpg"",""avatar_medium"":""m.jpg"",""avatar_big"":""l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("99999");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsFullNameAsLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""a@t.com"",""full_name"":""John Smith"",""avatar_small"":""s.jpg"",""avatar_medium"":""m.jpg"",""avatar_big"":""l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.LastName.Should().Be("John Smith");
            info.FirstName.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1234"",""email"":""user@todoist.com"",""full_name"":""Todo User"",""avatar_small"":""https://td.com/s.jpg"",""avatar_medium"":""https://td.com/m.jpg"",""avatar_big"":""https://td.com/l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("1234");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Large").GetString()
                .Should().Be("https://td.com/l.jpg");
        }

        private class TestableTodoistClient : TodoistClient
        {
            public TestableTodoistClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
