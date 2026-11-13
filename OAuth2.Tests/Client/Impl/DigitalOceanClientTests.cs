using System;
using System.Collections.Specialized;
using System.Net;
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
    public class DigitalOceanClientTests
    {
        /* lang=json */
        private const string Content = "{\"access_token\":\"yada\",\"token_type\":\"bearer\",\"expires_in\":2592000,\"refresh_token\":\"yada\",\"scope\":\"read\",\"uid\":123456,\"info\":{\"name\":\"first.last\",\"email\":\"first.last@domain.com\"}}";

        private DigitalOceanClientDescendant _descendant = null!;
        private IRequestFactory _requestFactory = null!;

        [SetUp]
        public void SetUp()
        {
            _requestFactory = Substitute.For<IRequestFactory>();
            _descendant = new DigitalOceanClientDescendant(
                _requestFactory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://cloud.digitalocean.com");
            endpoint.Resource.Should().Be("/v1/oauth/authorize");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://cloud.digitalocean.com");
            endpoint.Resource.Should().Be("/v1/oauth/token");
        }

        [Test]
        public void UserInfoEndpoint_Default_ThrowsNotImplemented()
        {
            // arrange

            // act & assert
            Assert.Throws<NotImplementedException>(() => _descendant.GetUserInfoServiceEndpoint());
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            // https://docs.digitalocean.com/reference/api/oauth/; credentials and profile values replaced.
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token","info":{"uuid":"00000000-0000-4000-8000-000000000001","team_uuid":"00000000-0000-4000-8000-000000000002","name":"Example User","email":"user@example.com"}}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://cloud.digitalocean.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://cloud.digitalocean.com").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new DigitalOceanClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", ClientPublic = "public-key",
                RedirectUri = "https://app.example.com/callback?next=a&value=b"
            });

            // Act
            var info = await client.GetUserInfoAsync(new NameValueCollection { { "code", "code+&=" } });

            // Assert
            handler.SentRequests.Should().HaveCount(1);
            var tokenRequest = handler.SentRequests[0];
            tokenRequest.Method.Should().Be(HttpMethod.Post);
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://cloud.digitalocean.com/v1/oauth/token");
            tokenRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
            var form = HttpUtility.ParseQueryString(handler.SentBodies[0]!);
            form["code"].Should().Be("code+&=");
            form["client_id"].Should().Be("client-id");
            form["client_secret"].Should().Be("secret+&=");
            form["grant_type"].Should().Be("authorization_code");
            form["redirect_uri"].Should().Be("https://app.example.com/callback?next=a&value=b");
            tokenRequest.Headers.Authorization.Should().BeNull();
            info.Id.Should().Be("00000000-0000-4000-8000-000000000001");
            info.ProviderName.Should().Be("DigitalOcean");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("123456");
            info.FirstName.Should().Be("first.last");
            info.LastName.Should().Be("");
            info.Email.Should().Be("first.last@domain.com");
        }

        class DigitalOceanClientDescendant : DigitalOceanClient
        {
            public DigitalOceanClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
