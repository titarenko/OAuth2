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
    public class AsanaClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableAsanaClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableAsanaClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectId()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""12345"",""name"":""John Doe"",""email"":""john@asana.com"",""photo"":{""image_36x36"":""https://asana.com/36.jpg"",""image_60x60"":""https://asana.com/60.jpg"",""image_128x128"":""https://asana.com/128.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("12345");
        }

        [Test]
        public void ParseUserInfo_TwoWordName_SplitsIntoFirstAndLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""John Doe"",""email"":""j@a.com"",""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Doe");
        }

        [Test]
        public void ParseUserInfo_ThreeWordName_FirstWordIsFirstNameRestIsLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""Mary Jane Watson"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Mary");
            info.LastName.Should().Be("Jane Watson");
        }

        [Test]
        public void ParseUserInfo_SingleWordName_LastNameIsEmpty()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""Madonna"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Madonna");
            info.LastName.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_ValidContent_MapsPhotoSizesToAvatarUri()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""A B"",""email"":null,""photo"":{""image_36x36"":""https://asana.com/36.jpg"",""image_60x60"":""https://asana.com/60.jpg"",""image_128x128"":""https://asana.com/128.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://asana.com/36.jpg");
            info.AvatarUri.Normal.Should().Be("https://asana.com/60.jpg");
            info.AvatarUri.Large.Should().Be("https://asana.com/128.jpg");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""A B"",""email"":""john@asana.com"",""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().Be("john@asana.com");
        }

        [Test]
        public void ParseUserInfo_MissingDataProperty_ReturnsEmptyUserInfo()
        {
            // arrange
            /* lang=json */
            const string content = @"{}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().BeNull();
            info.FirstName.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""12345"",""name"":""John Doe"",""email"":""john@asana.com"",""photo"":{""image_36x36"":""https://asana.com/36.jpg"",""image_60x60"":""https://asana.com/60.jpg"",""image_128x128"":""https://asana.com/128.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);

            // assert
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            root.GetProperty("Id").GetString().Should().Be("12345");
            root.GetProperty("FirstName").GetString().Should().Be("John");
            root.GetProperty("LastName").GetString().Should().Be("Doe");
            root.GetProperty("AvatarUri").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Test]
        public void ParseUserInfo_NullEmail_SerializesEmailAsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""A B"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Email").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Test]
        public void ParseUserInfo_NumericId_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":98765,""name"":""A B"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("98765");
        }

        private class TestableAsanaClient : AsanaClient
        {
            public TestableAsanaClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
