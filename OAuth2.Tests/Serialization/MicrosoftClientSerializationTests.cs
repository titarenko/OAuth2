using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2.Tests.TestHelpers;
using RestSharp;

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
        public async Task GetUserInfoAsync_GraphResponse_SendsBearerAndPreservesIdentity()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token","token_type":"Bearer"}""");
            // Graph /me response fields; identifiers and contact details are synthetic.
            // https://learn.microsoft.com/en-us/graph/api/user-get?view=graph-rest-1.0
            handler.EnqueueResponse(/* lang=json */ """{"id":"ms-123","givenName":"Test","surname":"User","mail":"user@example.com"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://login.microsoftonline.com"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://graph.microsoft.com"), disposeHttpClient: false);
            _factory.CreateClient("https://login.microsoftonline.com").Returns(tokenClient);
            _factory.CreateClient("https://graph.microsoft.com").Returns(userClient);
            _factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new MicrosoftClient(_factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "client-secret",
                RedirectUri = "https://app.example.com/callback", Scope = "User.Read"
            });

            // Act
            var info = await client.GetUserInfoAsync(new NameValueCollection { { "code", "test-code" } });

            // Assert
            handler.SentRequests.Should().HaveCount(2);
            var request = handler.SentRequests[1];
            request.RequestUri!.AbsoluteUri.Should().Be("https://graph.microsoft.com/v1.0/me");
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("test-token");
            info.ProviderName.Should().Be("Microsoft");
            info.Id.Should().Be("ms-123");
            info.Email.Should().Be("user@example.com");
            info.EmailVerified.Should().BeNull();
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
