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
    public class VSTSClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableVSTSClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableVSTSClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""vsts-guid-123"",""displayName"":""Dev User"",""emailAddress"":""dev@visualstudio.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("vsts-guid-123");
            info.FirstName.Should().Be("Dev User");
            info.Email.Should().Be("dev@visualstudio.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_FormatsAvatarUrlsWithIdAndSize()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""guid-123"",""displayName"":""A"",""emailAddress"":""a@v.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Contain("guid-123").And.Contain("small");
            info.AvatarUri.Normal.Should().Contain("guid-123").And.Contain("medium");
            info.AvatarUri.Large.Should().Contain("guid-123").And.Contain("large");
        }

        [Test]
        public void ParseUserInfo_ValidContent_AvatarUrlsPointToVsspsApi()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""guid-123"",""displayName"":""A"",""emailAddress"":""a@v.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().StartWith("https://app.vssps.visualstudio.com/_apis/Profile/Profiles/");
        }

        [Test]
        public void ParseUserInfo_ValidContent_LastNameIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""guid-123"",""displayName"":""Dev User"",""emailAddress"":""dev@v.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.LastName.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""vsts-guid-123"",""displayName"":""Dev User"",""emailAddress"":""dev@visualstudio.com""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("vsts-guid-123");
            doc.RootElement.GetProperty("FirstName").GetString().Should().Be("Dev User");
        }

        private class TestableVSTSClient : VSTSClient
        {
            public TestableVSTSClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
