using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Tests.Serialization
{
    [TestFixture]
    public class InstagramClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableInstagramClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableInstagramClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidTokenResponse_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string tokenContent = @"{""access_token"":""token123"",""user"":{""id"":""ig-1"",""username"":""johndoe"",""full_name"":""John Doe"",""profile_picture"":""https://ig.com/pic.jpg""}}";
            _client.SimulateAfterGetAccessToken(tokenContent);

            // act
            var info = _client.ParseUserInfo(string.Empty);

            // assert
            info.Id.Should().Be("ig-1");
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Doe");
        }

        [Test]
        public void ParseUserInfo_ValidTokenResponse_SetsOnlyNormalAvatar()
        {
            // arrange
            /* lang=json */
            const string tokenContent = @"{""access_token"":""t"",""user"":{""id"":""1"",""username"":""u"",""full_name"":""A B"",""profile_picture"":""https://ig.com/pic.jpg""}}";
            _client.SimulateAfterGetAccessToken(tokenContent);

            // act
            var info = _client.ParseUserInfo(string.Empty);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().Be("https://ig.com/pic.jpg");
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_SingleWordName_LastNameIsEmpty()
        {
            // arrange
            /* lang=json */
            const string tokenContent = @"{""access_token"":""t"",""user"":{""id"":""1"",""username"":""madonna"",""full_name"":""Madonna"",""profile_picture"":""p.jpg""}}";
            _client.SimulateAfterGetAccessToken(tokenContent);

            // act
            var info = _client.ParseUserInfo(string.Empty);

            // assert
            info.FirstName.Should().Be("Madonna");
            info.LastName.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_ValidTokenResponse_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string tokenContent = @"{""access_token"":""token123"",""user"":{""id"":""ig-1"",""username"":""johndoe"",""full_name"":""John Doe"",""profile_picture"":""https://ig.com/pic.jpg""}}";
            _client.SimulateAfterGetAccessToken(tokenContent);

            // act
            var info = _client.ParseUserInfo(string.Empty);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("ig-1");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString()
                .Should().Be("https://ig.com/pic.jpg");
        }

        private class TestableInstagramClient : InstagramClient
        {
            public TestableInstagramClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public void SimulateAfterGetAccessToken(string content)
            {
                AfterGetAccessToken(new BeforeAfterRequestArgs
                {
                    Response = new RestResponse { Content = content }
                });
            }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
