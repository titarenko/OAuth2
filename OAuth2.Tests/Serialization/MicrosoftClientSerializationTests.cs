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
    public class MicrosoftClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableMicrosoftClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableMicrosoftClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""ms-123"",""givenName"":""Jane"",""surname"":""Doe"",""mail"":""jane@outlook.com"",""displayName"":""Jane Doe"",""userPrincipalName"":""jane@outlook.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("ms-123");
            info.FirstName.Should().Be("Jane");
            info.LastName.Should().Be("Doe");
            info.Email.Should().Be("jane@outlook.com");
        }

        [Test]
        public void ParseUserInfo_NullMail_FallsBackToUserPrincipalName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""ms-456"",""givenName"":""John"",""surname"":""Smith"",""mail"":null,""userPrincipalName"":""john@contoso.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().Be("john@contoso.com");
        }

        [Test]
        public void ParseUserInfo_MissingMail_FallsBackToUserPrincipalName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""ms-789"",""givenName"":""Bob"",""surname"":""Jones"",""userPrincipalName"":""bob@contoso.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().Be("bob@contoso.com");
        }

        [Test]
        public void ParseUserInfo_MissingGivenName_ReturnsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""ms-111"",""surname"":""Only"",""mail"":""x@y.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().BeNull();
            info.LastName.Should().Be("Only");
        }

        [Test]
        public void ParseUserInfo_ValidContent_NameIsMicrosoft()
        {
            // assert
            _client.Name.Should().Be("Microsoft");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""ms-123"",""givenName"":""Jane"",""surname"":""Doe"",""mail"":""jane@outlook.com""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("ms-123");
            doc.RootElement.GetProperty("Email").GetString().Should().Be("jane@outlook.com");
        }

        private class TestableMicrosoftClient : MicrosoftClient
        {
            public TestableMicrosoftClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
