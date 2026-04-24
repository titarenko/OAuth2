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
    public class DigitalOceanClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableDigitalOceanClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableDigitalOceanClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectId()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":""do-123"",""info"":{""name"":""Jane Smith"",""email"":""jane@do.com""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("do-123");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsFirstNameToFullName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":""1"",""info"":{""name"":""Jane Smith"",""email"":""j@d.com""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Jane Smith");
            info.LastName.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":""1"",""info"":{""name"":""Test""}}";

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
            const string content = @"{""uid"":""do-123"",""info"":{""name"":""Jane Smith"",""email"":""jane@do.com""}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("do-123");
            doc.RootElement.GetProperty("FirstName").GetString().Should().Be("Jane Smith");
        }

        [Test]
        public void ParseUserInfo_NumericUid_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""uid"":42,""info"":{""name"":""Test"",""email"":""t@d.com""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("42");
        }

        private class TestableDigitalOceanClient : DigitalOceanClient
        {
            public TestableDigitalOceanClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
