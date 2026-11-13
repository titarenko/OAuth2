using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
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
    public class FacebookClientTests
    {
        /* lang=json */
        private const string Content = "{\"email\":\"email\",\"first_name\":\"name\",\"last_name\":\"surname\",\"id\":\"id\",\"picture\":{\"data\":{\"url\":\"picture\"}}}";

        private FacebookClientDescendant _descendant = null!;
        private IRequestFactory _factory = null!;
        private MockHttpMessageHandler _handler = null!;
        private List<RestRequest> _capturedRequests = null!;

        [SetUp]
        public void SetUp()
        {
            _handler = new MockHttpMessageHandler();
            _capturedRequests = new List<RestRequest>();

            _factory = Substitute.For<IRequestFactory>();
            _factory.CreateClient(Arg.Any<string>()).Returns(callInfo =>
                new RestClient(new HttpClient(_handler), new RestClientOptions(callInfo.Arg<string>())));
            _factory.CreateRequest(Arg.Any<string>()).Returns(callInfo =>
            {
                var req = new RestRequest(callInfo.Arg<string>());
                _capturedRequests.Add(req);
                return req;
            });
            _factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(callInfo =>
            {
                var req = new RestRequest(callInfo.Arg<string>(), callInfo.Arg<Method>());
                _capturedRequests.Add(req);
                return req;
            });

            _descendant = new FacebookClientDescendant(_factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.facebook.com");
            endpoint.Resource.Should().Be("/v25.0/dialog/oauth");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://graph.facebook.com");
            endpoint.Resource.Should().Be("/v25.0/oauth/access_token");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://graph.facebook.com");
            endpoint.Resource.Should().Be("/v25.0/me");
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Existing Facebook parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """{"id":"12345","first_name":"Example","last_name":"User","email":"user@example.com","picture":{"data":{"url":"https://example.com/photo"}}}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://graph.facebook.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://graph.facebook.com").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new FacebookClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://graph.facebook.com/v25.0/oauth/access_token");
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
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://graph.facebook.com/v25.0/me");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("OAuth test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            query["fields"].Should().Be("id,first_name,last_name,email,picture");
            info.Id.Should().Be("12345");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("id");
            info.FirstName.Should().Be("name");
            info.LastName.Should().Be("surname");
            info.Email.Should().Be("email");
            info.PhotoUri.Should().StartWith("picture");
        }

        [Test]
        public async Task GetUserInfo_Called_AddsFieldsParameter()
        {
            // arrange
            _handler.EnqueueResponse("access_token=token");
            _handler.EnqueueResponse(Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                { "code", "code" }
            });

            // assert
            var userInfoRequest = _capturedRequests.Last();
            userInfoRequest.Parameters.FirstOrDefault(p => String.Equals(p.Name, "fields", StringComparison.Ordinal))?.Value
                .Should().Be("id,first_name,last_name,email,picture");
        }

        class FacebookClientDescendant : FacebookClient
        {
            public FacebookClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
