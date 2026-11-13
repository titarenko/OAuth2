using System.Collections.Specialized;
using System.Net.Http;
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

namespace OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class LinkedInClientTests
    {
        // Legacy parser regression fixtures retained from the base branch.
        /* lang=json */
        private const string Content = @"{""sub"":""id"",""given_name"":""firstname"",""family_name"":""lastname"",""email"":""user@example.com"",""picture"":""https://media.licdn.com/photo.jpg""}";

        private LinkedInClientDescendant _descendant = null!;

        [SetUp]
        public void SetUp()
        {
            _descendant = new LinkedInClientDescendant(Substitute.For<IRequestFactory>(), Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // Arrange
            // The client is initialized in SetUp.

            // Act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // Assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/oauth/v2/authorization");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // Arrange
            // The client is initialized in SetUp.

            // Act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // Assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/oauth/v2/accessToken");
        }

        [Test]
        public async Task GetUserInfoAsync_DocumentedResponse_PreservesClaimsAndBearerHeader()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            // Documented token-response fields with a dummy access token.
            // https://learn.microsoft.com/en-us/linkedin/shared/authentication/authorization-code-flow
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token","expires_in":5184000}""");
            handler.EnqueueResponse(ProviderResponses.LinkedInUserInfo);
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://www.linkedin.com"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://api.linkedin.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://www.linkedin.com").Returns(tokenClient);
            factory.CreateClient("https://api.linkedin.com").Returns(userClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call =>
                new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var configuration = new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "client-secret",
                RedirectUri = "https://app.example.com/callback", Scope = "openid profile email"
            };
            var client = new LinkedInClient(factory, configuration);
            var parameters = new NameValueCollection { { "code", "code" }, { "state", "state" } };

            // Act
            var info = await client.GetUserInfoAsync(parameters);

            // Assert
            info.ProviderName.Should().Be("LinkedIn");
            info.Id.Should().Be("782bbtaQ");
            info.Email.Should().Be("doe@example.com");
            info.EmailVerified.Should().BeTrue();
            info.ProviderData.Should().BeNull();
            client.State.Should().Be("state");
            handler.SentRequests.Should().HaveCount(2);
            handler.SentRequests[0].RequestUri!.AbsoluteUri.Should().Be("https://www.linkedin.com/oauth/v2/accessToken");
            handler.SentRequests[1].RequestUri!.AbsoluteUri.Should().Be("https://api.linkedin.com/v2/userinfo");
            handler.SentRequests[1].Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.SentRequests[1].Headers.Authorization!.Parameter.Should().Be("test-token");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ParseUserInfo_BooleanVerification_PreservesClaim(bool verified)
        {
            // Arrange
            var response = JsonNode.Parse(ProviderResponses.LinkedInUserInfo)!.AsObject();
            response["email_verified"] = verified;
            var content = response.ToJsonString();

            // Act
            var info = _descendant.ParseUserInfo(content);

            // Assert
            info.Email.Should().Be("doe@example.com");
            info.EmailVerified.Should().Be(verified);
            info.ProviderData.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_MissingEmail_PreservesVerification()
        {
            // Arrange
            var response = JsonNode.Parse(ProviderResponses.LinkedInUserInfo)!.AsObject();
            response.Remove("email");
            var content = response.ToJsonString();

            // Act
            var info = _descendant.ParseUserInfo(content);

            // Assert
            info.Email.Should().BeNull();
            info.EmailVerified.Should().BeTrue();
        }

        [Test]
        public void ParseUserInfo_MissingVerification_ReturnsNull()
        {
            // Arrange
            var response = JsonNode.Parse(ProviderResponses.LinkedInUserInfo)!.AsObject();
            response.Remove("email_verified");
            var content = response.ToJsonString();

            // Act
            var info = _descendant.ParseUserInfo(content);

            // Assert
            info.Email.Should().Be("doe@example.com");
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
            var response = JsonNode.Parse(ProviderResponses.LinkedInUserInfo)!.AsObject();
            response["email_verified"] = JsonNode.Parse(claim);
            var content = response.ToJsonString();

            // Act
            var info = _descendant.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("782bbtaQ");
            info.Email.Should().Be("doe@example.com");
            info.EmailVerified.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_RepeatedResponses_DoesNotReuseVerification()
        {
            // Arrange
            var first = _descendant.ParseUserInfo(ProviderResponses.LinkedInUserInfo);
            var response = JsonNode.Parse(ProviderResponses.LinkedInUserInfo)!.AsObject();
            response.Remove("email_verified");
            var content = response.ToJsonString();

            // Act
            var second = _descendant.ParseUserInfo(content);

            // Assert
            first.EmailVerified.Should().BeTrue();
            second.EmailVerified.Should().BeNull();
            second.Should().NotBeSameAs(first);
        }

        [Test]
        public void ParseUserInfo_SourcedResponse_ReturnsCorrectFields()
        {
            // Arrange
            var content = ProviderResponses.LinkedInUserInfo;

            // Act
            var info = _descendant.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("782bbtaQ");
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Doe");
            info.Email.Should().Be("doe@example.com");
            info.EmailVerified.Should().BeTrue();
            info.PhotoUri.Should().Be("https://media.licdn-ei.com/dms/image/C5F03AQHqK8v7tB1HCQ/profile-displayphoto-shrink_100_100/0/");
            info.ProviderData.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // Arrange
            // Legacy synthetic payload, with its email replaced by a reserved example address.

            // Act
            var info = _descendant.ParseUserInfo(Content);

            // Assert
            info.Id.Should().Be("id");
            info.FirstName.Should().Be("firstname");
            info.LastName.Should().Be("lastname");
            info.Email.Should().Be("user@example.com");
            info.AvatarUri.Normal.Should().Be("https://media.licdn.com/photo.jpg");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // Arrange
            // The client is initialized in SetUp.

            // Act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // Assert
            endpoint.BaseUri.Should().Be("https://api.linkedin.com");
            endpoint.Resource.Should().Be("/v2/userinfo");
        }

        private class LinkedInClientDescendant : LinkedInClient
        {
            public LinkedInClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public Endpoint GetAccessCodeServiceEndpoint() => AccessCodeServiceEndpoint;
            public Endpoint GetAccessTokenServiceEndpoint() => AccessTokenServiceEndpoint;
            public Endpoint GetUserInfoServiceEndpoint() => UserInfoServiceEndpoint;
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
