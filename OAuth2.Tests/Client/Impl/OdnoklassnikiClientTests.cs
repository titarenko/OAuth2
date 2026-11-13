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
    [TestFixture]
    public class OdnoklassnikiClientTests
    {
        /* lang=json */
        private const string Content = "{\"uid\":\"12345\",\"first_name\":\"Oleg\",\"last_name\":\"Ivanov\",\"pic_1\":\"https://i.mycdn.me/image?id=123&photoType=4\"}";

        private OdnoklassnikiClientDescendant _descendant = null!;
        private IRequestFactory _factory = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _descendant = new OdnoklassnikiClientDescendant(
                _factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.odnoklassniki.ru");
            endpoint.Resource.Should().Be("/oauth/authorize");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.odnoklassniki.ru");
            endpoint.Resource.Should().Be("/oauth/token.do");
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Existing Odnoklassniki parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """{"uid":"12345","first_name":"Example","last_name":"User","pic_1":"https://example.com/photo"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://api.odnoklassniki.ru"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://api.odnoklassniki.ru").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new OdnoklassnikiClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://api.odnoklassniki.ru/oauth/token.do");
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
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://api.odnoklassniki.ru/fb.do");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("OAuth test-token");
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            query["access_token"].Should().Be("test-token");
            query["application_key"].Should().Be("public-key");
            query["method"].Should().Be("users.getCurrentUser");
            // Independent known-answer vector for https://apiok.ru/en/dev/methods/
            query["sig"].Should().Be("60b6bc9f0640d4cefabc5a87a96af594");
            info.Id.Should().Be("12345");
        }

        [Test]
        public async Task GetUserInfoAsync_CustomHeaders_ExcludesHeadersFromSignature()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            // Deliberate reserved characters in a synthetic token; existing parser fixture, not a live provider response.
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test+&=token"}""");
            handler.EnqueueResponse(Content);
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://api.odnoklassniki.ru"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://api.odnoklassniki.ru"), disposeHttpClient: false);
            userClient.AddDefaultHeader("X-Default-Correlation-Id", "default-correlation");
            var userRequest = new RestRequest("/fb.do");
            userRequest.AddHeader("X-Request-Correlation-Id", "request-correlation");
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://api.odnoklassniki.ru").Returns(tokenClient, userClient);
            factory.CreateRequest("/oauth/token.do", Method.Post).Returns(new RestRequest("/oauth/token.do", Method.Post));
            factory.CreateRequest("/fb.do", Method.Get).Returns(userRequest);
            var client = new OdnoklassnikiClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", ClientPublic = "public-key",
                RedirectUri = "https://app.example.com/callback"
            });

            // Act
            await client.GetUserInfoAsync(new NameValueCollection { { "code", "test-code" } });

            // Assert
            handler.SentRequests.Should().HaveCount(2);
            var request = handler.SentRequests[1];
            request.Method.Should().Be(HttpMethod.Get);
            request.Headers.GetValues("X-Default-Correlation-Id").Should().Equal("default-correlation");
            request.Headers.GetValues("X-Request-Correlation-Id").Should().Equal("request-correlation");
            var query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
            query.AllKeys.Should().BeEquivalentTo(new[] { "application_key", "method", "access_token", "sig" });
            query["access_token"].Should().Be("test+&=token");
            // Independent MD5 known answer for the decoded query, excluding access_token, headers and sig:
            // https://apiok.ru/en/dev/methods/ (Calculating request signature).
            query["sig"].Should().Be("919ab1390dc02b706643ef87ac8edec7");
        }

        [TestCase(false, ParameterType.GetOrPost)]
        [TestCase(false, ParameterType.QueryString)]
        [TestCase(true, ParameterType.GetOrPost)]
        [TestCase(true, ParameterType.QueryString)]
        public async Task GetUserInfoAsync_CustomQueryToken_SignsTransmittedParameters(bool clientDefault, ParameterType parameterType)
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            // Deliberate reserved characters in a synthetic token; existing parser fixture, not a live provider response.
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test+&=token"}""");
            handler.EnqueueResponse(Content);
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://api.odnoklassniki.ru"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://api.odnoklassniki.ru"), disposeHttpClient: false);
            var userRequest = new RestRequest("/fb.do");
            if (clientDefault)
                userClient.AddDefaultParameter("oauth_token", "test+&=token", parameterType);
            else
                userRequest.AddParameter("oauth_token", "test+&=token", parameterType);

            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://api.odnoklassniki.ru").Returns(tokenClient, userClient);
            factory.CreateRequest("/oauth/token.do", Method.Post).Returns(new RestRequest("/oauth/token.do", Method.Post));
            factory.CreateRequest("/fb.do", Method.Get).Returns(userRequest);
            var client = new OdnoklassnikiClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", ClientPublic = "public-key",
                RedirectUri = "https://app.example.com/callback"
            });

            // Act
            await client.GetUserInfoAsync(new NameValueCollection { { "code", "test-code" } });

            // Assert
            handler.SentRequests.Should().HaveCount(2);
            var request = handler.SentRequests[1];
            request.Method.Should().Be(HttpMethod.Get);
            var query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
            query.AllKeys.Should().BeEquivalentTo(new[] { "application_key", "method", "oauth_token", "access_token", "sig" });
            query.GetValues("oauth_token").Should().Equal("test+&=token");
            query["access_token"].Should().Be("test+&=token");
            // Independent MD5 known answer for the decoded query, including oauth_token but excluding access_token:
            // https://apiok.ru/en/dev/methods/ (Calculating request signature).
            query["sig"].Should().Be("1ea163e3a981a4f4d4bae30939796533");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("12345");
            info.FirstName.Should().Be("Oleg");
            info.LastName.Should().Be("Ivanov");
            info.AvatarUri.Large.Should().Be("https://i.mycdn.me/image?id=123&photoType=6");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // Arrange

            // Act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // Assert
            endpoint.BaseUri.Should().Be("https://api.odnoklassniki.ru");
            endpoint.Resource.Should().Be("/fb.do");
        }

        private class OdnoklassnikiClientDescendant : OdnoklassnikiClient
        {
            public OdnoklassnikiClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
