using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2.Tests.TestHelpers;
using RestSharp;

namespace OAuth2.Tests.Serialization
{
    [TestFixture]
    public class SalesforceClientSerializationTests
    {
        private TestableSalesforceClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _client = new TestableSalesforceClient(Substitute.For<IRequestFactory>(), Substitute.For<IClientConfiguration>());
        }

        [TestCase("https://login.salesforce.com")]
        [TestCase("https://test.salesforce.com")]
        [TestCase("https://example.my.site.com")]
        public async Task GetUserInfoAsync_IdentityResponse_PreservesIdentityUrlAndBearerHeader(string identityBaseUri)
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            // Documented identity URL variations for production, sandbox, and Experience Cloud.
            // https://help.salesforce.com/s/articleView?id=sf.remoteaccess_using_openid.htm&language=en_US&type=0
            var identityUrl = identityBaseUri + "/id/some-org-id/some-user-id";
            handler.EnqueueResponse(JsonSerializer.Serialize(new { access_token = "test-token", token_type = "Bearer", id = identityUrl }));
            var response = JsonNode.Parse(ProviderResponses.SalesforceIdentity)!.AsObject();
            response["id"] = identityUrl;
            handler.EnqueueResponse(response.ToJsonString());
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://login.salesforce.com"), disposeHttpClient: false);
            using var identityClient = new RestClient(httpClient, new RestClientOptions(identityBaseUri), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://login.salesforce.com").Returns(tokenClient);
            factory.CreateClient(identityBaseUri).Returns(identityClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call =>
                new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var configuration = new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "client-secret",
                RedirectUri = "https://app.example.com/callback"
            };
            var client = new SalesforceClient(factory, configuration);
            var parameters = new NameValueCollection { { "code", "code" }, { "state", "state" } };

            // Act
            var info = await client.GetUserInfoAsync(parameters);

            // Assert
            info.ProviderName.Should().Be("Salesforce");
            info.Id.Should().Be(identityUrl);
            info.Email.Should().Be("user@example.com");
            info.EmailVerified.Should().BeTrue();
            info.ProviderData.Should().BeNull();
            client.SalesforceProfileUrl.Should().Be(identityUrl);
            client.State.Should().Be("state");
            handler.SentRequests.Should().HaveCount(2);
            handler.SentRequests[0].RequestUri!.AbsoluteUri.Should().Be("https://login.salesforce.com/services/oauth2/token");
            handler.SentRequests[1].RequestUri!.AbsoluteUri.Should().Be(identityUrl);
            handler.SentRequests[1].Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.SentRequests[1].Headers.Authorization!.Parameter.Should().Be("test-token");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("file:///identity.json")]
        [TestCase("http://login.salesforce.com/id/some-org-id/some-user-id")]
        [TestCase("//login.salesforce.com/id/some-org-id/some-user-id")]
        [TestCase("not-a-url")]
        public async Task GetUserInfoAsync_NonHttpsIdentityUrl_RejectsBeforeSendingToken(string? identityUrl)
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            // Deliberately invalid identity URLs must not receive the access token.
            handler.EnqueueResponse(JsonSerializer.Serialize(new { access_token = "test-token", token_type = "Bearer", id = identityUrl }));
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://login.salesforce.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://login.salesforce.com").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call =>
                new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new SalesforceClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "client-secret",
                RedirectUri = "https://app.example.com/callback"
            });
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act & Assert
            var exception = await client.Awaiting(x => x.GetUserInfoAsync(parameters)).Should().ThrowAsync<UnexpectedResponseException>();
            exception.Which.FieldName.Should().Be("id");
            handler.SentRequests.Should().ContainSingle();
            handler.SentRequests[0].RequestUri!.AbsoluteUri.Should().Be("https://login.salesforce.com/services/oauth2/token");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ParseUserInfo_BooleanVerification_PreservesClaim(bool verified)
        {
            // Arrange
            var response = JsonNode.Parse(ProviderResponses.SalesforceIdentity)!.AsObject();
            response["email_verified"] = verified;
            var content = response.ToJsonString();

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Email.Should().Be("user@example.com");
            info.EmailVerified.Should().Be(verified);
            info.ProviderData.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_MissingEmail_PreservesVerification()
        {
            // Arrange
            var response = JsonNode.Parse(ProviderResponses.SalesforceIdentity)!.AsObject();
            response.Remove("email");
            var content = response.ToJsonString();

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Email.Should().BeNull();
            info.EmailVerified.Should().BeTrue();
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // Arrange
            // Legacy synthetic payload with relative photo paths and no email claim.
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""photos"":{""thumbnail"":""t.jpg"",""picture"":""p.jpg""}}";

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_MissingVerification_ReturnsNull()
        {
            // Arrange
            var response = JsonNode.Parse(ProviderResponses.SalesforceIdentity)!.AsObject();
            response.Remove("email_verified");
            var content = response.ToJsonString();

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Email.Should().Be("user@example.com");
            info.EmailVerified.Should().BeNull();
        }

        [TestCase(/* lang=json */ "null")]
        [TestCase(/* lang=json */ "\"true\"")]
        [TestCase(/* lang=json */ "1")]
        [TestCase(/* lang=json */ "{}")]
        [TestCase(/* lang=json */ "[]")]
        public void ParseUserInfo_NonBooleanVerification_ReturnsNull(string claim)
        {
            // Arrange
            // Deliberately invalid claim types, not successful API response examples.
            var response = JsonNode.Parse(ProviderResponses.SalesforceIdentity)!.AsObject();
            response["email_verified"] = JsonNode.Parse(claim);
            var content = response.ToJsonString();

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("https://login.salesforce.com/id/some-org-id/some-user-id");
            info.Email.Should().Be("user@example.com");
            info.EmailVerified.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_RepeatedResponses_DoesNotReuseVerification()
        {
            // Arrange
            var first = _client.ParseUserInfo(ProviderResponses.SalesforceIdentity);
            var response = JsonNode.Parse(ProviderResponses.SalesforceIdentity)!.AsObject();
            response.Remove("email_verified");
            var content = response.ToJsonString();

            // Act
            var second = _client.ParseUserInfo(content);

            // Assert
            first.EmailVerified.Should().BeTrue();
            second.EmailVerified.Should().BeNull();
            second.Should().NotBeSameAs(first);
        }

        [Test]
        public void ParseUserInfo_SourcedResponse_ReturnsCorrectFields()
        {
            // Arrange
            var content = ProviderResponses.SalesforceIdentity;

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("https://login.salesforce.com/id/some-org-id/some-user-id");
            info.FirstName.Should().Be("Example");
            info.LastName.Should().Be("User");
            info.Email.Should().Be("user@example.com");
            info.EmailVerified.Should().BeTrue();
            info.ProviderData.Should().BeNull();
            info.AvatarUri.Small.Should().Be("https://example.com/profilephoto/thumb");
            info.AvatarUri.Normal.Should().Be("https://example.com/profilephoto/full");
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_SourcedResponse_SerializesToValidJson()
        {
            // Arrange
            var content = ProviderResponses.SalesforceIdentity;

            // Act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // Assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("https://login.salesforce.com/id/some-org-id/some-user-id");
            doc.RootElement.GetProperty("Email").GetString().Should().Be("user@example.com");
            doc.RootElement.GetProperty("EmailVerified").GetBoolean().Should().BeTrue();
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Small").GetString().Should().Be("https://example.com/profilephoto/thumb");
        }

        [Test]
        public void ParseUserInfo_ValidContent_MapsPhotosToAvatarUri()
        {
            // Arrange
            // Legacy synthetic payload; email and photo hosts use reserved example domains.
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""a@example.com"",""first_name"":""A"",""last_name"":""B"",""photos"":{""thumbnail"":""https://example.com/thumb.jpg"",""picture"":""https://example.com/pic.jpg""}}";

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.AvatarUri.Small.Should().Be("https://example.com/thumb.jpg");
            info.AvatarUri.Normal.Should().Be("https://example.com/pic.jpg");
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // Arrange
            // Legacy synthetic payload; email and photo hosts use reserved example domains.
            /* lang=json */
            const string content = @"{""id"":""sf-123"",""email"":""admin@example.com"",""first_name"":""Sales"",""last_name"":""Admin"",""photos"":{""thumbnail"":""https://example.com/thumb.jpg"",""picture"":""https://example.com/pic.jpg""}}";

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("sf-123");
            info.FirstName.Should().Be("Sales");
            info.LastName.Should().Be("Admin");
            info.Email.Should().Be("admin@example.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // Arrange
            // Legacy synthetic payload; email and photo hosts use reserved example domains.
            /* lang=json */
            const string content = @"{""id"":""sf-123"",""email"":""admin@example.com"",""first_name"":""Sales"",""last_name"":""Admin"",""photos"":{""thumbnail"":""https://example.com/thumb.jpg"",""picture"":""https://example.com/pic.jpg""}}";

            // Act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // Assert
            doc.RootElement.GetProperty("Email").GetString().Should().Be("admin@example.com");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Small").GetString()
                .Should().Be("https://example.com/thumb.jpg");
        }

        private class TestableSalesforceClient : SalesforceClient
        {
            public TestableSalesforceClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
