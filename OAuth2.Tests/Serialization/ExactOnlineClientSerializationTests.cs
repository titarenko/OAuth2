using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
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
    public class ExactOnlineClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableExactOnlineClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableExactOnlineClient(_factory, _configuration);
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Exact's System/Me schema in an OData collection; values replaced with examples.
            // https://start.exactonline.nl/docs/HlpRestAPIResourcesDetails.aspx?name=SystemSystemMe
            handler.EnqueueResponse(/* lang=json */ """{"d":{"results":[{"UserID":"00000000-0000-4000-8000-000000000001","FirstName":"Example","LastName":"User","FullName":"Example User","Email":"user@example.com","PictureUrl":"https://example.com/photo"}]}}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://start.exactonline.nl"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://start.exactonline.nl").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new ExactOnlineClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", ClientPublic = "public-key",
                RedirectUri = "https://app.example.com/callback?next=a&value=b"
            });

            // Act
            var loginUri = new System.Uri(await client.GetLoginLinkUriAsync("state+&="));
            var info = await client.GetUserInfoAsync(new NameValueCollection { { "code", "code+&=" } });

            // Assert
            loginUri.GetLeftPart(System.UriPartial.Path).Should().Be("https://start.exactonline.nl/api/oauth2/auth");
            HttpUtility.ParseQueryString(loginUri.Query)["state"].Should().Be("state+&=");
            handler.SentRequests.Should().HaveCount(2);
            var tokenRequest = handler.SentRequests[0];
            tokenRequest.Method.Should().Be(HttpMethod.Post);
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://start.exactonline.nl/api/oauth2/token");
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
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://start.exactonline.nl/api/v1/current/Me");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            request.Headers.Accept.Should().ContainSingle(value => value.MediaType == "application/json");
            info.Id.Should().Be("00000000-0000-4000-8000-000000000001");
            info.FirstName.Should().Be("Example");
            info.LastName.Should().Be("User");
            info.Email.Should().Be("user@example.com");
            info.AvatarUri.Normal.Should().Be("https://example.com/photo");
            info.ProviderName.Should().Be(client.Name);
        }

        [TestCase("{\"d\":{\"results\":[]}}")]
        [TestCase("{\"d\":{\"results\":[{\"FullName\":\"Example User\"}]}}")]
        [TestCase("{\"d\":{\"results\":[{\"UserID\":{}}]}}")]
        public void ParseUserInfo_ODataWithoutUserId_RejectsResponse(string content)
        {
            // Arrange
            // Deliberate mutations of the System/Me response: no user or no primary key.

            // Act & Assert
            Assert.Throws<UnexpectedResponseException>(() => _client.ParseUserInfo(content));
        }

        [Test]
        public void ParseUserInfo_SingleODataEntity_MapsUserIdWithoutOptionalFields()
        {
            // Arrange
            // Exact's OData single-record wrapper; optional System/Me fields omitted deliberately.
            // https://www.exactsoftware.com/docs/DocView.aspx?DocumentID={b3ae2566-b48c-461c-8788-b6e756a44798}
            const string content = /* lang=json */ """{"d":{"UserID":"00000000-0000-4000-8000-000000000001"}}""";

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("00000000-0000-4000-8000-000000000001");
            info.FirstName.Should().BeNull();
            info.LastName.Should().BeNull();
            info.Email.Should().BeNull();
            info.EmailVerified.Should().BeNull();
            info.AvatarUri.Normal.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectId()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""exact-1"",""display_name"":""Alice"",""email"":""alice@exact.com"",""images"":[{""url"":""https://exact.com/avatar.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("exact-1");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsFirstNameFromDisplayName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""Alice"",""email"":""a@e.com"",""images"":[{""url"":""pic.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Alice");
        }

        [Test]
        public void ParseUserInfo_WithImages_SetsAllAvatarSizesToSameUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""A"",""email"":""a@e.com"",""images"":[{""url"":""https://exact.com/avatar.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://exact.com/avatar.jpg");
            info.AvatarUri.Normal.Should().Be("https://exact.com/avatar.jpg");
            info.AvatarUri.Large.Should().Be("https://exact.com/avatar.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingImages_AvatarUriIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""Bob"",""email"":""b@e.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsProviderName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""A"",""email"":""a@e.com"",""images"":[{""url"":""pic.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.ProviderName.Should().Be("ExactOnline");
        }

        [Test]
        public void ParseUserInfo_NumericId_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":12345,""display_name"":""A"",""email"":""a@e.com"",""images"":[{""url"":""pic.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("12345");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""exact-1"",""display_name"":""Alice"",""email"":""alice@exact.com"",""images"":[{""url"":""https://exact.com/avatar.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("exact-1");
            doc.RootElement.GetProperty("AvatarUri").ValueKind.Should().Be(JsonValueKind.Object);
        }

        private class TestableExactOnlineClient : ExactOnlineClient
        {
            public TestableExactOnlineClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
