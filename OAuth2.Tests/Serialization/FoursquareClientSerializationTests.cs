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
    public class FoursquareClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableFoursquareClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableFoursquareClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":{""user"":{""id"":""4sq-1"",""firstName"":""Alice"",""lastName"":""Wonder"",""contact"":{""email"":""alice@4sq.com""},""photo"":{""prefix"":""https://4sq.com/img/"",""suffix"":""/photo.jpg""}}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("4sq-1");
            info.FirstName.Should().Be("Alice");
            info.LastName.Should().Be("Wonder");
            info.Email.Should().Be("alice@4sq.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsSmallAvatarWith36x36()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":{""user"":{""id"":""1"",""firstName"":""A"",""lastName"":""B"",""contact"":{},""photo"":{""prefix"":""https://4sq.com/img/"",""suffix"":""/photo.jpg""}}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://4sq.com/img/36x36/photo.jpg");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsNormalAvatarWithNoSize()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":{""user"":{""id"":""1"",""firstName"":""A"",""lastName"":""B"",""contact"":{},""photo"":{""prefix"":""https://4sq.com/img/"",""suffix"":""/photo.jpg""}}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Be("https://4sq.com/img//photo.jpg");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsLargeAvatarWith300x300()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":{""user"":{""id"":""1"",""firstName"":""A"",""lastName"":""B"",""contact"":{},""photo"":{""prefix"":""https://4sq.com/img/"",""suffix"":""/photo.jpg""}}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Large.Should().Be("https://4sq.com/img/300x300/photo.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":{""user"":{""id"":""1"",""firstName"":""A"",""lastName"":""B"",""contact"":{},""photo"":{""prefix"":""p/"",""suffix"":"".jpg""}}}}";

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
            const string content = @"{""response"":{""user"":{""id"":""4sq-1"",""firstName"":""Alice"",""lastName"":""Wonder"",""contact"":{""email"":""alice@4sq.com""},""photo"":{""prefix"":""https://4sq.com/img/"",""suffix"":""/photo.jpg""}}}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("4sq-1");
            doc.RootElement.GetProperty("AvatarUri").ValueKind.Should().Be(JsonValueKind.Object);
        }

        private class TestableFoursquareClient : FoursquareClient
        {
            public TestableFoursquareClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
