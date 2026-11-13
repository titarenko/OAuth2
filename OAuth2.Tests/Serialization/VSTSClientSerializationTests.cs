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
    public class VSTSClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableVSTSClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableVSTSClient(_factory, _configuration);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task GetCurrentTokenAsync_RefreshToken_SendsEncodedRefreshGrant(bool jsonResponse)
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            // Synthetic token responses exercise both supported encodings.
            handler.EnqueueResponse(jsonResponse
                ? /* lang=json */ """{"access_token":"next+token","refresh_token":"refresh+next"}"""
                : "access_token=next%2Btoken&refresh_token=refresh%2Bnext");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var restClient = new RestClient(httpClient, new RestClientOptions("https://app.vssps.visualstudio.com/oauth2"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://app.vssps.visualstudio.com/oauth2").Returns(restClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new TestableVSTSClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=",
                RedirectUri = "https://app.example.com/callback"
            });

            // Act
            var token = await client.GetCurrentTokenAsync("refresh+&=", forceUpdate: true);

            // Assert
            handler.SentRequests.Should().ContainSingle();
            var request = handler.SentRequests[0];
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.Query.Should().BeEmpty();
            request.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
            var form = HttpUtility.ParseQueryString(handler.SentBodies[0]!);
            form["grant_type"].Should().Be("refresh_token");
            form["code"].Should().BeNull();
            form["assertion"].Should().Be("refresh+&=");
            form["client_assertion"].Should().Be("secret+&=");
            form["client_assertion_type"].Should().Be("urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
            form["redirect_uri"].Should().Be("https://app.example.com/callback");
            form["refresh_token"].Should().BeNull();
            request.Headers.Authorization.Should().BeNull();
            token.Should().Be("next+token");
            client.RefreshToken.Should().Be("refresh+next");
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Existing VSTS parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """{"id":"vsts-guid-123","displayName":"Dev User","emailAddress":"user@example.com"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://app.vssps.visualstudio.com/oauth2"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://app.vssps.visualstudio.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://app.vssps.visualstudio.com/oauth2").Returns(tokenClient);
            factory.CreateClient("https://app.vssps.visualstudio.com").Returns(userClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new TestableVSTSClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://app.vssps.visualstudio.com/oauth2/token");
            tokenRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
            var form = HttpUtility.ParseQueryString(handler.SentBodies[0]!);
            form["assertion"].Should().Be("code+&=");
            form["client_assertion"].Should().Be("secret+&=");
            form["client_assertion_type"].Should().Be("urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
            form["grant_type"].Should().Be("urn:ietf:params:oauth:grant-type:jwt-bearer");
            form["code"].Should().BeNull();
            form["redirect_uri"].Should().Be("https://app.example.com/callback?next=a&value=b");
            tokenRequest.Headers.Authorization.Should().BeNull();
            var request = handler.SentRequests[1];
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://app.vssps.visualstudio.com/_apis/profile/profiles/me");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            query["api-version"].Should().Be("1.0");
            info.ProviderName.Should().Be(client.Name);
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

#pragma warning disable CS0618 // Type or member is obsolete
        private class TestableVSTSClient : VSTSClient
        {
            public TestableVSTSClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
#pragma warning restore CS0618
    }
}
