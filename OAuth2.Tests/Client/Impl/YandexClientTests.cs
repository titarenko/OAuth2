using System.Collections.Specialized;
using System.Net.Http;
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

namespace OAuth2.Tests.Client.Impl
{
    public class YandexClientTests
    {
        /* lang=json */
        private const string Content = "{\"id\": \"349\", \"login\": \"mylogin\", \"client_id\": \"e0000000000000000000191f3280bb\", \"display_name\": \"My Name\", \"real_name\": \"Real Name\", \"first_name\": \"Real\", \"last_name\": \"Name\", \"default_email\": \"mymail@yandex.ru\", \"emails\": [\"mymail@yandex.ru\"], \"default_avatar_id\": \"\", \"is_avatar_empty\": true, \"psuid\": \"1.AA.XXXXXXXXXXXXXXXXXXXXXXX.YYYYYYYYYYYYYYYYYYYYYYYYY\"}";
        /* lang=json */
        private const string ContentWithAvatar = "{\"id\": \"349\", \"login\": \"mylogin\", \"client_id\": \"e0000000000000000000191f3280bb\", \"display_name\": \"My Name\", \"real_name\": \"Real Name\", \"first_name\": \"Real\", \"last_name\": \"Name\", \"default_email\": \"mymail@yandex.ru\", \"emails\": [\"mymail@yandex.ru\"], \"default_avatar_id\": \"1111/enc-000\", \"is_avatar_empty\": false, \"psuid\": \"1.AA.XXXXXXXXXXXXXXXXXXXXXXX.YYYYYYYYYYYYYYYYYYYYYYYYY\"}";

        private YandexClientDescendant _descendant = null!;
        private IRequestFactory _factory = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _descendant = new YandexClientDescendant(
                _factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.yandex.ru");
            endpoint.Resource.Should().Be("/authorize");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.yandex.ru");
            endpoint.Resource.Should().Be("/token");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://login.yandex.ru");
            endpoint.Resource.Should().Be("/info");
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Existing Yandex parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """{"id":"12345","real_name":"Example User","display_name":"Example","default_email":"user@example.com","default_avatar_id":"12345"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://oauth.yandex.ru"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://login.yandex.ru"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://oauth.yandex.ru").Returns(tokenClient);
            factory.CreateClient("https://login.yandex.ru").Returns(userClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new YandexClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://oauth.yandex.ru/token");
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
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://login.yandex.ru/info");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("OAuth test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            info.Id.Should().Be("12345");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("349");
            info.AvatarUri.Normal.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_ContentWithAvatar_ReturnsCorrectAvatarUri()
        {
            // arrange (uses ContentWithAvatar const)

            // act
            var info = _descendant.ParseUserInfo(ContentWithAvatar);

            // assert
            info.Id.Should().Be("349");
            info.AvatarUri.Normal.Should().Be("https://avatars.yandex.net/get-yapic/1111/enc-000/islands-retina-50");
        }

        private class YandexClientDescendant : YandexClient
        {
            public YandexClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration)
            {
            }

            public Endpoint GetAccessCodeServiceEndpoint()
            {
                return AccessCodeServiceEndpoint;
            }

            public Endpoint GetAccessTokenServiceEndpoint()
            {
                return AccessTokenServiceEndpoint;
            }

            public Endpoint GetUserInfoServiceEndpoint()
            {
                return UserInfoServiceEndpoint;
            }

            public new UserInfo ParseUserInfo(string content)
            {
                return base.ParseUserInfo(content);
            }
        }
    }
}
