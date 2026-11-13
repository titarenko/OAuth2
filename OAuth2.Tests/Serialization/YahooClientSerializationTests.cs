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
    public class YahooClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableYahooClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableYahooClient(_factory, _configuration);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests(bool includesLegacyGuid)
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(includesLegacyGuid
                ? /* lang=json */ """{"access_token":"test-token","xoauth_yahoo_guid":"test-guid"}"""
                : /* lang=json */ """{"access_token":"test-token"}""");
            // https://developer.yahoo.com/oauth/social-directory-eol/; profile values replaced.
            handler.EnqueueResponse(/* lang=json */ """{"sub":"test-guid","given_name":"Example","family_name":"User","email":"user@example.com","email_verified":true,"profile_images":{"image32":"https://example.com/32.jpg","image64":"https://example.com/64.jpg","image128":"https://example.com/128.jpg"},"picture":"https://example.com/192.jpg"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://api.login.yahoo.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://api.login.yahoo.com").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new YahooClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", ClientPublic = "public-key",
                RedirectUri = "https://app.example.com/callback?next=a&value=b", Scope = "openid profile email"
            });

            // Act
            var loginUri = new System.Uri(await client.GetLoginLinkUriAsync("state+&="));
            var info = await client.GetUserInfoAsync(new NameValueCollection { { "code", "code+&=" } });

            // Assert
            loginUri.GetLeftPart(System.UriPartial.Path).Should().Be("https://api.login.yahoo.com/oauth2/request_auth");
            var loginQuery = HttpUtility.ParseQueryString(loginUri.Query);
            loginQuery["scope"].Should().Be("openid profile email");
            loginQuery["state"].Should().Be("state+&=");
            handler.SentRequests.Should().HaveCount(2);
            var tokenRequest = handler.SentRequests[0];
            tokenRequest.Method.Should().Be(HttpMethod.Post);
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://api.login.yahoo.com/oauth2/get_token");
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
            request.RequestUri!.AbsoluteUri.Should().Be("https://api.login.yahoo.com/openid/v1/userinfo");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            query["format"].Should().BeNull();
            info.Id.Should().Be("test-guid");
            client.UserProfileGUID.Should().Be(info.Id);
            info.FirstName.Should().Be("Example");
            info.LastName.Should().Be("User");
            info.Email.Should().Be("user@example.com");
            info.EmailVerified.Should().BeTrue();
            info.AvatarUri.Small.Should().Be("https://example.com/32.jpg");
            info.AvatarUri.Normal.Should().Be("https://example.com/64.jpg");
            info.AvatarUri.Large.Should().Be("https://example.com/128.jpg");
            info.ProviderName.Should().Be(client.Name);
        }

        [TestCase("true", true)]
        [TestCase("false", false)]
        [TestCase("null", null)]
        [TestCase("\"true\"", null)]
        public void ParseUserInfo_CurrentEmailVerification_PreservesOnlyBooleanAssertions(string verification, bool? expected)
        {
            // Arrange
            // Deliberately vary Yahoo's documented email_verified, including malformed text.
            const string template = /* lang=json */ """{"sub":"test-guid","email":"user@example.com","email_verified":VERIFICATION}""";

            // Act
            var info = _client.ParseUserInfo(template.Replace("VERIFICATION", verification));

            // Assert
            info.EmailVerified.Should().Be(expected);
        }

        [TestCase("{}")]
        [TestCase("{\"sub\":null}")]
        [TestCase("{\"sub\":\"\"}")]
        [TestCase("{\"sub\":123}")]
        public void ParseUserInfo_InvalidSubject_RejectsResponse(string content)
        {
            // Arrange
            // Deliberate mutations: UserInfo must contain a nonempty string subject.

            // Act & Assert
            Assert.Throws<UnexpectedResponseException>(() => _client.ParseUserInfo(content));
        }

        [Test]
        public void ParseUserInfo_MismatchedTokenGuid_RejectsResponse()
        {
            // Arrange
            // Deliberately mismatch the token's historical GUID and the UserInfo subject.
            _client.SetTokenResponse(/* lang=json */ """{"xoauth_yahoo_guid":"token-guid"}""");
            const string content = /* lang=json */ """{"sub":"different-guid","email":"user@example.com"}""";

            // Act & Assert
            Assert.Throws<UnexpectedResponseException>(() => _client.ParseUserInfo(content));
        }

        [TestCase("\"https://example.com/profile\"")]
        [TestCase("{\"givenName\":\"Legacy name\"}")]
        public void ParseUserInfo_OpenIdSubjectWithProfileClaim_PrefersCurrentClaims(string profile)
        {
            // Arrange
            // profile is an OIDC URL claim; the second case deliberately mixes old and new shapes.
            const string template = /* lang=json */ """{"sub":"test-guid","given_name":"Example","profile":PROFILE}""";

            // Act
            var info = _client.ParseUserInfo(template.Replace("PROFILE", profile));

            // Assert
            info.Id.Should().Be("test-guid");
            info.FirstName.Should().Be("Example");
        }

        [Test]
        public void ParseUserInfo_PictureWithoutSizedImages_UsesPictureForAllSizes()
        {
            // Arrange
            // OIDC UserInfo example with synthetic values and no Yahoo-specific profile_images.
            // https://developer.yahoo.com/oauth2/guide/openid_connect/getting_user_info.html
            const string content = /* lang=json */ """{"sub":"test-guid","picture":"https://example.com/photo"}""";

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("test-guid");
            info.Email.Should().BeNull();
            info.EmailVerified.Should().BeNull();
            info.AvatarUri.Small.Should().Be("https://example.com/photo");
            info.AvatarUri.Normal.Should().Be("https://example.com/photo");
            info.AvatarUri.Large.Should().Be("https://example.com/photo");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectNames()
        {
            // arrange
            /* lang=json */
            const string content = @"{""profile"":{""givenName"":""John"",""familyName"":""Yahoo"",""image"":{""imageUrl"":""https://yahoo.com/pic.jpg""}},""emails"":""john@yahoo.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Yahoo");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsAllAvatarSizesToSameUrl()
        {
            // arrange
            /* lang=json */
            const string content = @"{""profile"":{""givenName"":""A"",""familyName"":""B"",""image"":{""imageUrl"":""https://yahoo.com/pic.jpg""}},""emails"":""a@y.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://yahoo.com/pic.jpg");
            info.AvatarUri.Normal.Should().Be("https://yahoo.com/pic.jpg");
            info.AvatarUri.Large.Should().Be("https://yahoo.com/pic.jpg");
        }

        [Test]
        public void ParseUserInfo_MissingImage_AvatarUriIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""profile"":{""givenName"":""A"",""familyName"":""B""},""emails"":""a@y.com""}";

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
            const string content = @"{""profile"":{""givenName"":""A"",""familyName"":""B"",""image"":{""imageUrl"":""pic.jpg""}},""emails"":""a@y.com""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.ProviderName.Should().Be("Yahoo");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""profile"":{""givenName"":""John"",""familyName"":""Yahoo"",""image"":{""imageUrl"":""https://yahoo.com/pic.jpg""}},""emails"":""john@yahoo.com""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("ProviderName").GetString().Should().Be("Yahoo");
            doc.RootElement.GetProperty("AvatarUri").ValueKind.Should().Be(JsonValueKind.Object);
        }

        private class TestableYahooClient : YahooClient
        {
            public TestableYahooClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);

            public void SetTokenResponse(string content) => AfterGetAccessToken(new BeforeAfterRequestArgs
            {
                Response = new RestResponse { Content = content }
            });
        }
    }
}
