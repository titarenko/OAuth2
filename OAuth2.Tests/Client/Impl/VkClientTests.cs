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
    public class VkClientTests
    {
        /* lang=json */
        private const string Content = "{\"response\":[{\"id\":\"1\",\"first_name\":\"Павел\",\"last_name\":\"Дуров\",\"has_photo\":1,\"photo_max_orig\":\"http:\\/\\/cs109.vkontakte.ru\\/u00001\\/c_df2abf56.jpg\"}]}";

        private VkClientDescendant _descendant = null!;
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

            _descendant = new VkClientDescendant(_factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.vk.com");
            endpoint.Resource.Should().Be("/authorize");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.vk.com");
            endpoint.Resource.Should().Be("/access_token");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.vk.com");
            endpoint.Resource.Should().Be("/method/users.get");
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token","user_id":12345,"email":"user@example.com"}""");
            // Existing Vk parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """{"response":[{"id":12345,"first_name":"Example","last_name":"User","has_photo":0}]}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://oauth.vk.com"), disposeHttpClient: false);
            using var userClient = new RestClient(httpClient, new RestClientOptions("https://api.vk.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://oauth.vk.com").Returns(tokenClient);
            factory.CreateClient("https://api.vk.com").Returns(userClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new VkClient(factory, new ClientConfiguration
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
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://oauth.vk.com/access_token");
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
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://api.vk.com/method/users.get");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization.Should().BeNull();
            query["access_token"].Should().Be("test-token");
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            query["v"].Should().Be("5.131");
            query["user_ids"].Should().Be("12345");
            query["fields"].Should().Be("first_name,last_name,has_photo,photo_max_orig");
            info.Id.Should().Be("12345");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("1");
            info.FirstName.Should().Be("Павел");
            info.LastName.Should().Be("Дуров");
            info.Email.Should().BeNull();
            info.PhotoUri.Should().Be("http://cs109.vkontakte.ru/u00001/c_df2abf56.jpg");
        }

        [Test]
        public async Task AfterGetAccessToken_ValidResponse_SetsUserId()
        {
            // arrange
            _handler.EnqueueResponse(/* lang=json */ "{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}");
            _handler.EnqueueResponse(Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                { "code", "code" }
            });

            // assert
            var userInfoRequest = _capturedRequests.Last();
            userInfoRequest.Parameters.FirstOrDefault(p => String.Equals(p.Name, "user_ids", StringComparison.Ordinal))?.Value
                .Should().Be("1");
        }

        [Test]
        public async Task GetUserInfo_Called_AddsFieldsAndVersionParameters()
        {
            // arrange
            _handler.EnqueueResponse(/* lang=json */ "{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}");
            _handler.EnqueueResponse(Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                { "code", "code" }
            });

            // assert
            var userInfoRequest = _capturedRequests.Last();
            userInfoRequest.Parameters.FirstOrDefault(p => String.Equals(p.Name, "fields", StringComparison.Ordinal))?.Value
                .Should().Be("first_name,last_name,has_photo,photo_max_orig");
            userInfoRequest.Parameters.FirstOrDefault(p => String.Equals(p.Name, "user_ids", StringComparison.Ordinal))?.Value
                .Should().Be("1");
            userInfoRequest.Parameters.FirstOrDefault(p => String.Equals(p.Name, "v", StringComparison.Ordinal))?.Value
                .Should().Be("5.131");
        }

        private class VkClientDescendant : VkClient
        {
            public VkClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
