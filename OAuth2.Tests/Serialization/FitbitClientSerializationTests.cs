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
    public class FitbitClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableFitbitClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableFitbitClient(_factory, _configuration);
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
            using var restClient = new RestClient(httpClient, new RestClientOptions("https://api.fitbit.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://api.fitbit.com").Returns(restClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new FitbitClient(factory, new ClientConfiguration
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
            form["refresh_token"].Should().Be("refresh+&=");
            form["client_id"].Should().Be("client-id");
            form["client_secret"].Should().Be("secret+&=");
            form["redirect_uri"].Should().BeNull();
            request.Headers.Authorization!.ToString().Should().Be("Basic Y2xpZW50LWlkOnNlY3JldCsmPQ==");
            token.Should().Be("next+token");
            client.RefreshToken.Should().Be("refresh+next");
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Existing Fitbit parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """{"user":{"encodedId":"fitbit-1","fullName":"Jane Doe","displayName":"JaneDoe","avatar":"https://example.com/photo"}}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://api.fitbit.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://api.fitbit.com").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new FitbitClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://api.fitbit.com/oauth2/token");
            tokenRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
            var form = HttpUtility.ParseQueryString(handler.SentBodies[0]!);
            form["code"].Should().Be("code+&=");
            form["client_id"].Should().Be("client-id");
            form["client_secret"].Should().Be("secret+&=");
            form["grant_type"].Should().Be("authorization_code");
            form["redirect_uri"].Should().Be("https://app.example.com/callback?next=a&value=b");
            tokenRequest.Headers.Authorization!.ToString().Should().Be("Basic Y2xpZW50LWlkOnNlY3JldCsmPQ==");
            var request = handler.SentRequests[1];
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://api.fitbit.com/1/user/-/profile.json");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            info.ProviderName.Should().Be(client.Name);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectId()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""fitbit-1"",""fullName"":""Jane Doe"",""displayName"":""JaneDoe"",""avatar"":""https://fitbit.com/avatar.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("fitbit-1");
        }

        [Test]
        public void ParseUserInfo_TwoWordName_SplitsIntoFirstAndLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""1"",""fullName"":""Jane Doe"",""displayName"":""JaneDoe"",""avatar"":""a.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Jane");
            info.LastName.Should().Be("Doe");
        }

        [Test]
        public void ParseUserInfo_SingleWordName_LastNameIsEmpty()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""1"",""fullName"":""Cher"",""displayName"":""CherFitness"",""avatar"":""a.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Cher");
            info.LastName.Should().BeEmpty();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsOnlyNormalAvatar()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""1"",""fullName"":""A B"",""displayName"":""AB"",""avatar"":""https://fitbit.com/avatar.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().Be("https://fitbit.com/avatar.jpg");
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""fitbit-1"",""fullName"":""Jane Doe"",""displayName"":""JaneDoe"",""avatar"":""https://fitbit.com/avatar.jpg""}}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("fitbit-1");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString()
                .Should().Be("https://fitbit.com/avatar.jpg");
        }

        private class TestableFitbitClient : FitbitClient
        {
            public TestableFitbitClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
