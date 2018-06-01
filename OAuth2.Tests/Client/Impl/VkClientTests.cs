using System.Collections.Specialized;
using System.Net;
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
using RestSharp;

namespace OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class VkClientTests
    {
        private const string Content = "{\"response\":[{\"id\":\"1\",\"first_name\":\"Павел\",\"last_name\":\"Дуров\",\"has_photo\":1,\"photo_max_orig\":\"http:\\/\\/cs109.vkontakte.ru\\/u00001\\/c_df2abf56.jpg\"}]}";

        private VkClientDescendant _descendant;

        private IRequestFactory _factory;
        private IRestClient _restClient;
        private IRestRequest _restRequest;
        private IRestResponse _restResponse;

        [SetUp]
        public void SetUp()
        {
            _restRequest = Substitute.For<IRestRequest>();
            _restResponse = Substitute.For<IRestResponse>();

            _restResponse.StatusCode.Returns(HttpStatusCode.OK);
            _restResponse.Content.Returns("response");

            _restClient = Substitute.For<IRestClient>();
            _restClient.ExecuteTaskAsync(_restRequest, CancellationToken.None).Returns(_restResponse);

            _factory = Substitute.For<IRequestFactory>();
            _factory.CreateClient().Returns(_restClient);
            _factory.CreateRequest().Returns(_restRequest);

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
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            var response = (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None));

            response.Content.Returns(
                "any content to pass response verification",
                "{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}",
                "{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}",
                Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                {"code", "code"}
            });

            // assert
            var notUsed = response.Received().Content;
        }

        [Test]
        public async Task Should_AddExtraParameters_WhenOnGetUserInfoIsCalled()
        {
            // arrange
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).Content.Returns(
                "any content to pass response verification",
                "{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}", 
                "{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}", 
                Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                {"code", "code"}
            });

            // assert
            restRequest.Received().AddParameter("fields", "first_name,last_name,has_photo,photo_max_orig");
            restRequest.Received().AddParameter("user_ids", "1");
            restRequest.Received().AddParameter("v", "5.74");
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