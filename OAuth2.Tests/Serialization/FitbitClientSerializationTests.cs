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
    public class FitbitClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableFitbitClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableFitbitClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectId()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""fitbit-1"",""fullName"":""Jane Doe"",""displayName"":""JaneDoe"",""avatar"":""https://fitbit.com/avatar.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("fitbit-1");
        }

        [Test]
        public void ParseUserInfo_TwoWordName_SplitsIntoFirstAndLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""1"",""fullName"":""Jane Doe"",""displayName"":""JaneDoe"",""avatar"":""a.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Jane");
            info.LastName.Should().Be("Doe");
        }

        [Test]
        public void ParseUserInfo_SingleWordName_LastNameIsEmpty()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""1"",""fullName"":""Cher"",""displayName"":""CherFitness"",""avatar"":""a.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Cher");
            info.LastName.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsOnlyNormalAvatar()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""1"",""fullName"":""A B"",""displayName"":""AB"",""avatar"":""https://fitbit.com/avatar.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().Be("https://fitbit.com/avatar.jpg");
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""fitbit-1"",""fullName"":""Jane Doe"",""displayName"":""JaneDoe"",""avatar"":""https://fitbit.com/avatar.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("fitbit-1");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString()
                .Should().Be("https://fitbit.com/avatar.jpg");
        }

        private class TestableFitbitClient : FitbitClient
        {
            public TestableFitbitClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
