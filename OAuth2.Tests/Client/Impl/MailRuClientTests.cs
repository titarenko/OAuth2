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
    public class MailRuClientTests
    {
        /* lang=json */
        private const string Content = "[{\"uid\":\"12345\",\"first_name\":\"Ivan\",\"last_name\":\"Petrov\",\"email\":\"ivan@mail.ru\",\"pic\":\"https://avt.appsmail.ru/mail/photo.jpg\"}]";

        private MailRuClientDescendant _descendant = null!;
        private IRequestFactory _factory = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _descendant = new MailRuClientDescendant(
                _factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://connect.mail.ru");
            endpoint.Resource.Should().Be("/oauth/authorize");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://connect.mail.ru");
            endpoint.Resource.Should().Be("/oauth/token");
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Existing MailRu parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """[{"uid":"12345","first_name":"Example","last_name":"User","email":"user@example.com","pic":"https://example.com/photo"}]""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://connect.mail.ru"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://www.appsmail.ru"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://connect.mail.ru").Returns(tokenClient);
            factory.CreateClient("https://www.appsmail.ru").Returns(userClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new MailRuClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://connect.mail.ru/oauth/token");
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
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://www.appsmail.ru/platform/api");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("OAuth test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            query["session_key"].Should().Be("test-token");
            query["app_id"].Should().Be("client-id");
            query["method"].Should().Be("users.getInfo");
            query["secure"].Should().Be("1");
            // Independent known-answer vector for https://api.mail.ru/docs/guides/restapi/
            query["sig"].Should().Be("da97f2ef49bcb265d9ace5c986b48fa3");
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
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://connect.mail.ru"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://www.appsmail.ru"), disposeHttpClient: false);
            userClient.AddDefaultHeader("X-Default-Correlation-Id", "default-correlation");
            var userRequest = new RestRequest("/platform/api");
            userRequest.AddHeader("X-Request-Correlation-Id", "request-correlation");
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://connect.mail.ru").Returns(tokenClient);
            factory.CreateClient("https://www.appsmail.ru").Returns(userClient);
            factory.CreateRequest("/oauth/token", Method.Post).Returns(new RestRequest("/oauth/token", Method.Post));
            factory.CreateRequest("/platform/api", Method.Get).Returns(userRequest);
            var client = new MailRuClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", RedirectUri = "https://app.example.com/callback"
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
            query.AllKeys.Should().BeEquivalentTo(new[] { "app_id", "method", "secure", "session_key", "sig" });
            query["session_key"].Should().Be("test+&=token");
            // Independent MD5 known answer for the decoded query, excluding headers and sig:
            // https://api.mail.ru/docs/guides/restapi/ (server-to-server signing).
            query["sig"].Should().Be("67209d4ac12c9ad3587803c9ecc843d7");
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
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://connect.mail.ru"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://www.appsmail.ru"), disposeHttpClient: false);
            var userRequest = new RestRequest("/platform/api");
            if (clientDefault)
                userClient.AddDefaultParameter("oauth_token", "test+&=token", parameterType);
            else
                userRequest.AddParameter("oauth_token", "test+&=token", parameterType);

            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://connect.mail.ru").Returns(tokenClient);
            factory.CreateClient("https://www.appsmail.ru").Returns(userClient);
            factory.CreateRequest("/oauth/token", Method.Post).Returns(new RestRequest("/oauth/token", Method.Post));
            factory.CreateRequest("/platform/api", Method.Get).Returns(userRequest);
            var client = new MailRuClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", RedirectUri = "https://app.example.com/callback"
            });

            // Act
            await client.GetUserInfoAsync(new NameValueCollection { { "code", "test-code" } });

            // Assert
            handler.SentRequests.Should().HaveCount(2);
            var request = handler.SentRequests[1];
            request.Method.Should().Be(HttpMethod.Get);
            var query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
            query.AllKeys.Should().BeEquivalentTo(new[] { "app_id", "method", "oauth_token", "secure", "session_key", "sig" });
            query.GetValues("oauth_token").Should().Equal("test+&=token");
            query["session_key"].Should().Be("test+&=token");
            // Independent MD5 known answer for the decoded query, including the real oauth_token:
            // https://api.mail.ru/docs/guides/restapi/ (server-to-server signing).
            query["sig"].Should().Be("d6100b22a76aeb94ae7582002d04ccdf");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("12345");
            info.FirstName.Should().Be("Ivan");
            info.LastName.Should().Be("Petrov");
            info.Email.Should().Be("ivan@mail.ru");
            info.PhotoUri.Should().Be("https://avt.appsmail.ru/mail/photo.jpg");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // Arrange

            // Act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // Assert
            endpoint.BaseUri.Should().Be("https://www.appsmail.ru");
            endpoint.Resource.Should().Be("/platform/api");
        }

        private class MailRuClientDescendant : MailRuClient
        {
            public MailRuClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
