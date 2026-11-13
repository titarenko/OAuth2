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
    public class TodoistClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableTodoistClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableTodoistClient(_factory, _configuration);
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Existing Todoist parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """{"id":"1234","email":"user@example.com","full_name":"Todo User","avatar_small":"https://example.com/photo","avatar_medium":"https://example.com/photo","avatar_big":"https://example.com/photo"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://api.todoist.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://api.todoist.com").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new TodoistClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://api.todoist.com/oauth/access_token");
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
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://api.todoist.com/api/v1/user");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            info.ProviderName.Should().Be(client.Name);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1234"",""email"":""user@todoist.com"",""full_name"":""Todo User"",""avatar_small"":""https://td.com/s.jpg"",""avatar_medium"":""https://td.com/m.jpg"",""avatar_big"":""https://td.com/l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("1234");
            info.Email.Should().Be("user@todoist.com");
            info.LastName.Should().Be("Todo User");
        }

        [Test]
        public void ParseUserInfo_ValidContent_MapsAvatarSizesToCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""a@t.com"",""full_name"":""A"",""avatar_small"":""https://td.com/s.jpg"",""avatar_medium"":""https://td.com/m.jpg"",""avatar_big"":""https://td.com/l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Small.Should().Be("https://td.com/s.jpg");
            info.AvatarUri.Normal.Should().Be("https://td.com/m.jpg");
            info.AvatarUri.Large.Should().Be("https://td.com/l.jpg");
        }

        [Test]
        public void ParseUserInfo_StringId_ReturnsIdAsIs()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""99999"",""email"":""a@t.com"",""full_name"":""A"",""avatar_small"":""s.jpg"",""avatar_medium"":""m.jpg"",""avatar_big"":""l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("99999");
        }

        [Test]
        public void ParseUserInfo_NumericId_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":99999,""email"":""a@t.com"",""full_name"":""A"",""avatar_small"":""s.jpg"",""avatar_medium"":""m.jpg"",""avatar_big"":""l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("99999");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsFullNameAsLastName()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""a@t.com"",""full_name"":""John Smith"",""avatar_small"":""s.jpg"",""avatar_medium"":""m.jpg"",""avatar_big"":""l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.LastName.Should().Be("John Smith");
            info.FirstName.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""id"":""1234"",""email"":""user@todoist.com"",""full_name"":""Todo User"",""avatar_small"":""https://td.com/s.jpg"",""avatar_medium"":""https://td.com/m.jpg"",""avatar_big"":""https://td.com/l.jpg""}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("1234");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Large").GetString()
                .Should().Be("https://td.com/l.jpg");
        }

        private class TestableTodoistClient : TodoistClient
        {
            public TestableTodoistClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
