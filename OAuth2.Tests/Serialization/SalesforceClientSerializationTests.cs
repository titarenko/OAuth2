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
    public class SalesforceClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableSalesforceClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableSalesforceClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""sf-123"",""email"":""admin@sf.com"",""first_name"":""Sales"",""last_name"":""Admin"",""photos"":{""thumbnail"":""https://sf.com/thumb.jpg"",""picture"":""https://sf.com/pic.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("sf-123");
            info.FirstName.Should().Be("Sales");
            info.LastName.Should().Be("Admin");
            info.Email.Should().Be("admin@sf.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_MapsPhotosToAvatarUri()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""a@s.com"",""first_name"":""A"",""last_name"":""B"",""photos"":{""thumbnail"":""https://sf.com/thumb.jpg"",""picture"":""https://sf.com/pic.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://sf.com/thumb.jpg");
            info.AvatarUri.Normal.Should().Be("https://sf.com/pic.jpg");
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""photos"":{""thumbnail"":""t.jpg"",""picture"":""p.jpg""}}";

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
            const string content = @"{""id"":""sf-123"",""email"":""admin@sf.com"",""first_name"":""Sales"",""last_name"":""Admin"",""photos"":{""thumbnail"":""https://sf.com/thumb.jpg"",""picture"":""https://sf.com/pic.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Email").GetString().Should().Be("admin@sf.com");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Small").GetString()
                .Should().Be("https://sf.com/thumb.jpg");
        }

        private class TestableSalesforceClient : SalesforceClient
        {
            public TestableSalesforceClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
