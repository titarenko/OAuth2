using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
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
    public class UberClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableUberClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableUberClient(_factory, _configuration);
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // https://developer.uber.com/docs/consumer-identity/references/api/v3/me-get
            // Profile example with the subject, email and image replaced with synthetic values.
            handler.EnqueueResponse(/* lang=json */ """{"sub":"example-uber-subject","given_name":"Uber","family_name":"Rider","email":"user@example.com","picture":"https://example.com/photo"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://auth.uber.com"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://api.uber.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://auth.uber.com").Returns(tokenClient);
            factory.CreateClient("https://api.uber.com").Returns(userClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new UberClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", ClientPublic = "public-key",
                RedirectUri = "https://app.example.com/callback?next=a&value=b"
            });

            // Act
            var info = await client.GetUserInfoAsync(new NameValueCollection { { "code", "code+&=" } });

            // Assert
            handler.SentRequests.Should().HaveCount(2);
            var tokenRequest = handler.SentRequests[0];
            tokenRequest.Method.Should().Be(HttpMethod.Post);
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://auth.uber.com/oauth/v2/token");
            tokenRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
            var form = HttpUtility.ParseQueryString(handler.SentBodies[0]!);
            form["code"].Should().Be("code+&=");
            form["client_id"].Should().Be("client-id");
            form["client_secret"].Should().Be("secret+&=");
            form["grant_type"].Should().Be("authorization_code");
            form["redirect_uri"].Should().Be("https://app.example.com/callback?next=a&value=b");
            tokenRequest.Headers.Authorization.Should().BeNull();
            var request = handler.SentRequests[1];
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://api.uber.com/v3/me");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            info.Id.Should().Be("example-uber-subject");
            info.FirstName.Should().Be("Uber");
            info.LastName.Should().Be("Rider");
            info.EmailVerified.Should().BeNull();
            info.ProviderName.Should().Be(client.Name);
        }

        [TestCase("true", true)]
        [TestCase("false", false)]
        [TestCase("null", null)]
        [TestCase("\"true\"", null)]
        public void ParseUserInfo_CurrentEmailVerification_PreservesOnlyBooleanAssertions(string verification, bool? expected)
        {
            // Arrange
            // v3/me example; deliberately vary the verification claim, including malformed text.
            const string template = /* lang=json */ """{"sub":"example-subject","email":"user@example.com","email_verified":VERIFICATION}""";

            // Act
            var info = _client.ParseUserInfo(template.Replace("VERIFICATION", verification));

            // Assert
            info.Id.Should().Be("example-subject");
            info.EmailVerified.Should().Be(expected);
        }

        [Test]
        public void ParseUserInfo_SubOnly_ReturnsIdWithoutInventingProfileClaims()
        {
            // Arrange
            // Documented v3/me response for eats.pos_provisioning without profile scope; subject replaced.
            const string content = /* lang=json */ """{"sub":"example-subject"}""";

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("example-subject");
            info.FirstName.Should().BeNull();
            info.LastName.Should().BeNull();
            info.Email.Should().BeNull();
            info.EmailVerified.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Uber"",""last_name"":""Rider"",""email"":""rider@uber.com"",""picture"":""https://uber.com/pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Uber");
            info.LastName.Should().Be("Rider");
            info.Email.Should().Be("rider@uber.com");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsAllAvatarSizesToSameUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""A"",""last_name"":""B"",""email"":""a@u.com"",""picture"":""https://uber.com/pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://uber.com/pic.jpg");
            info.AvatarUri.Normal.Should().Be("https://uber.com/pic.jpg");
            info.AvatarUri.Large.Should().Be("https://uber.com/pic.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingAllFields_ReturnsEmptyUserInfo()
        {
            // arrange
            /* lang=json */
            const string content = @"{}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().BeNull();
            info.LastName.Should().BeNull();
            info.Email.Should().BeNull();
            info.AvatarUri.Small.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_PartialFields_ReturnsOnlyPresentFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Test""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Test");
            info.LastName.Should().BeNull();
            info.Email.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_CaseInsensitiveProperties_MatchesKeys()
        {
            // arrange
            /* lang=json */
            const string content = @"{""First_Name"":""Uber"",""Last_Name"":""Rider"",""Email"":""rider@uber.com"",""Picture"":""https://uber.com/pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Uber");
            info.LastName.Should().Be("Rider");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Uber"",""last_name"":""Rider"",""email"":""rider@uber.com"",""picture"":""https://uber.com/pic.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("FirstName").GetString().Should().Be("Uber");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString()
                .Should().Be("https://uber.com/pic.jpg");
        }

        private class TestableUberClient : UberClient
        {
            public TestableUberClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
