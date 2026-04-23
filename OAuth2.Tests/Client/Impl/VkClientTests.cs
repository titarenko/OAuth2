using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        private const string Content = /* lang=json */ "{\"response\":[{\"id\":\"1\",\"first_name\":\"Павел\",\"last_name\":\"Дуров\",\"has_photo\":1,\"photo_max_orig\":\"http:\\/\\/cs109.vkontakte.ru\\/u00001\\/c_df2abf56.jpg\"}]}";

        private VkClientDescendant _descendant;
        private IRequestFactory _factory;
        private MockHttpMessageHandler _handler;
        private List<RestRequest> _capturedRequests;

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
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("http://oauth.vk.com");
            endpoint.Resource.Should().Be("/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.vk.com");
            endpoint.Resource.Should().Be("/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.vk.com");
            endpoint.Resource.Should().Be("/method/users.get");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
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
        public async Task Should_ReceiveUserId_WhenAccessTokenResponseReceived()
        {
            // arrange
            _handler.EnqueueResponse("{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}");
            _handler.EnqueueResponse(Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                { "code", "code" }
            });

            // assert
            var userInfoRequest = _capturedRequests.Last();
            userInfoRequest.Parameters.FirstOrDefault(p => p.Name == "user_ids")?.Value
                .Should().Be("1");
        }

        [Test]
        public async Task Should_AddExtraParameters_WhenOnGetUserInfoIsCalled()
        {
            // arrange
            _handler.EnqueueResponse("{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}");
            _handler.EnqueueResponse(Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                { "code", "code" }
            });

            // assert
            var userInfoRequest = _capturedRequests.Last();
            userInfoRequest.Parameters.FirstOrDefault(p => p.Name == "fields")?.Value
                .Should().Be("first_name,last_name,has_photo,photo_max_orig");
            userInfoRequest.Parameters.FirstOrDefault(p => p.Name == "user_ids")?.Value
                .Should().Be("1");
            userInfoRequest.Parameters.FirstOrDefault(p => p.Name == "v")?.Value
                .Should().Be("5.74");
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