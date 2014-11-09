﻿using System.Collections.Specialized;
using System.Linq;
using System.Net;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Portable;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class VkClientTests
    {
        private const string content = "{\"response\":[{\"uid\":\"1\",\"first_name\":\"Павел\",\"last_name\":\"Дуров\",\"photo\":\"http:\\/\\/cs109.vkontakte.ru\\/u00001\\/c_df2abf56.jpg\"}]}";

        private VkClientDescendant descendant;
        private IRequestFactory factory;

        private static System.Text.Encoding _encoding = System.Text.Encoding.UTF8;

        [SetUp]
        public void SetUp()
        {
            factory = Substitute.For<IRequestFactory>();
            var client = Substitute.For<IRestClient>();
            var request = Substitute.For<IRestRequest>();
            var response = Substitute.For<IRestResponse>();
            factory.CreateClient().Returns(client);
            factory.CreateRequest(null).ReturnsForAnyArgs(request);
            client.Execute(request).Returns(Task.FromResult(response));
            response.StatusCode.Returns(HttpStatusCode.OK);
            descendant = new VkClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("http://oauth.vk.com");
            endpoint.Resource.Should().Be("/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.vk.com");
            endpoint.Resource.Should().Be("/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.vk.com");
            endpoint.Resource.Should().Be("/method/users.get");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = descendant.ParseUserInfo(content);

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
            var response = await factory.CreateClient().Execute(factory.CreateRequest(null));
            response.GetContent().Returns(
                "any content to pass response verification",
                "{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}",
                "{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}",
                content);

            // act
            await descendant.GetUserInfo(new Dictionary<string, string>
            {
                {"code", "code"}
            }.ToLookup(y => y.Key, y => y.Value));

            // assert
            var notUsed = response.Received().GetContent();
        }

        [Test]
        public async Task Should_AddExtraParameters_WhenOnGetUserInfoIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest(null);
            (await restClient.Execute(restRequest)).RawBytes.Returns(
                _encoding.GetBytes("any content to pass response verification"),
                _encoding.GetBytes("{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}"), 
                _encoding.GetBytes("{\"access_token\":\"token\",\"expires_in\":0,\"user_id\":1}"), 
                _encoding.GetBytes(content));

            // act
            await descendant.GetUserInfo(new Dictionary<string, string>
            {
                {"code", "code"}
            }.ToLookup(y => y.Key, y => y.Value));

            // assert
            restRequest.Parameters.Received().Add(Arg.Is<Parameter>(x => x.Name == "fields" && (string)x.Value == "uid,first_name,last_name,photo"));
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