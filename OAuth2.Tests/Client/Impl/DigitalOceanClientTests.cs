using System;
using System.Net;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class DigitalOceanClientTests
    {
        private const string Content = "{\"access_token\":\"yada\",\"token_type\":\"bearer\",\"expires_in\":2592000,\"refresh_token\":\"yada\",\"scope\":\"read\",\"uid\":123456,\"info\":{\"name\":\"first.last\",\"email\":\"first.last@domain.com\"}}";

        private DigitalOceanClientDescendant _descendant;
        private IRequestFactory _requestFactory;

        [SetUp]
        public void SetUp()
        {
            _requestFactory = Substitute.For<IRequestFactory>();
            _requestFactory.CreateClient().Execute(_requestFactory.CreateRequest()).StatusCode = HttpStatusCode.OK;
            _descendant = new DigitalOceanClientDescendant(
                _requestFactory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://cloud.digitalocean.com");
            endpoint.Resource.Should().Be("/v1/oauth/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://cloud.digitalocean.com");
            endpoint.Resource.Should().Be("/v1/oauth/token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            Assert.Throws<NotImplementedException>(() => _descendant.GetUserInfoServiceEndpoint());
        }
        
        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = _descendant.ParseUserInfo(Content);

            //  assert
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