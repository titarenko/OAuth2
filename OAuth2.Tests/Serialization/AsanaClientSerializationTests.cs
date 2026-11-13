using System;
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
    public class AsanaClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableAsanaClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableAsanaClient(_factory, _configuration);
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // https://developers.asana.com/reference/getuser; profile values replaced with examples.
            handler.EnqueueResponse(/* lang=json */ """{"data":{"gid":"12345","resource_type":"user","name":"Example User","email":"user@example.com"}}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://app.asana.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://app.asana.com").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new AsanaClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://app.asana.com/-/oauth_token");
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
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://app.asana.com/api/1.0/users/me");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            query["opt_fields"].Should().Be("gid,name,photo.image_128x128,photo.image_60x60,photo.image_36x36,email");
            info.Id.Should().Be("12345");
        }

        [Test]
        public void ParseUserInfo_GidWithPartialPhoto_PreservesIdentityAndAvailablePhoto()
        {
            // Arrange
            // Asana's user schema; deliberate omission of optional photo sizes and email.
            // https://developers.asana.com/reference/getuser
            const string content = /* lang=json */ """{"data":{"gid":"9007199254740993","name":"Example User","photo":{"image_128x128":"https://example.com/128.jpg"}}}""";

            // Act
            var info = _client.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("9007199254740993");
            info.FirstName.Should().Be("Example");
            info.LastName.Should().Be("User");
            info.Email.Should().BeNull();
            info.AvatarUri.Small.Should().BeNullOrEmpty();
            info.AvatarUri.Normal.Should().BeNullOrEmpty();
            info.AvatarUri.Large.Should().Be("https://example.com/128.jpg");
        }

        [Test]
        public void ParseUserInfo_LegacyAndCurrentIds_ReturnsSameAccountKey()
        {
            // Arrange
            // Asana guarantees that existing IDs do not change when converted to strings.
            // https://forum.asana.com/t/asana-is-moving-to-string-ids-updated-with-revised-timeline/29340
            const string legacy = /* lang=json */ """{"data":{"id":9007199254740993,"name":"Example User"}}""";
            const string current = /* lang=json */ """{"data":{"gid":"9007199254740993","name":"Example User"}}""";

            // Act
            var legacyInfo = _client.ParseUserInfo(legacy);
            var currentInfo = _client.ParseUserInfo(current);

            // Assert
            currentInfo.Id.Should().Be(legacyInfo.Id).And.Be("9007199254740993");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectId()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""12345"",""name"":""John Doe"",""email"":""john@asana.com"",""photo"":{""image_36x36"":""https://asana.com/36.jpg"",""image_60x60"":""https://asana.com/60.jpg"",""image_128x128"":""https://asana.com/128.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("12345");
        }

        [Test]
        public void ParseUserInfo_TwoWordName_SplitsIntoFirstAndLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""John Doe"",""email"":""j@a.com"",""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Doe");
        }

        [Test]
        public void ParseUserInfo_ThreeWordName_FirstWordIsFirstNameRestIsLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""Mary Jane Watson"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Mary");
            info.LastName.Should().Be("Jane Watson");
        }

        [Test]
        public void ParseUserInfo_SingleWordName_LastNameIsEmpty()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""Madonna"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Madonna");
            info.LastName.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_ValidContent_MapsPhotoSizesToAvatarUri()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""A B"",""email"":null,""photo"":{""image_36x36"":""https://asana.com/36.jpg"",""image_60x60"":""https://asana.com/60.jpg"",""image_128x128"":""https://asana.com/128.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://asana.com/36.jpg");
            info.AvatarUri.Normal.Should().Be("https://asana.com/60.jpg");
            info.AvatarUri.Large.Should().Be("https://asana.com/128.jpg");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""A B"",""email"":""john@asana.com"",""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().Be("john@asana.com");
        }

        [Test]
        public void ParseUserInfo_MissingDataProperty_ReturnsEmptyUserInfo()
        {
            // arrange
            /* lang=json */
            const string content = @"{}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().BeNull();
            info.FirstName.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""12345"",""name"":""John Doe"",""email"":""john@asana.com"",""photo"":{""image_36x36"":""https://asana.com/36.jpg"",""image_60x60"":""https://asana.com/60.jpg"",""image_128x128"":""https://asana.com/128.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);

            // assert
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            root.GetProperty("Id").GetString().Should().Be("12345");
            root.GetProperty("FirstName").GetString().Should().Be("John");
            root.GetProperty("LastName").GetString().Should().Be("Doe");
            root.GetProperty("AvatarUri").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Test]
        public void ParseUserInfo_NullEmail_SerializesEmailAsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""A B"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Email").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Test]
        public void ParseUserInfo_NumericId_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""data"":{""id"":98765,""name"":""A B"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("98765");
        }

        [Test]
        public void ParseUserInfo_NullPhoto_DoesNotThrow()
        {
            // arrange — Asana returns null for photo when the user has no profile picture (issue #103)
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""John Doe"",""email"":""j@a.com"",""photo"":null}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("1");
            info.FirstName.Should().Be("John");
            info.AvatarUri.Small.Should().BeNullOrEmpty();
            info.AvatarUri.Normal.Should().BeNullOrEmpty();
            info.AvatarUri.Large.Should().BeNullOrEmpty();
        }

        private class TestableAsanaClient : AsanaClient
        {
            public TestableAsanaClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
