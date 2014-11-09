using System;
using System.Collections.Specialized;
using System.Net;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using System.Threading.Tasks;
using RestSharp.Portable;

namespace OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class DigitalOceanClientTests
    {
        private const string content = "{\"access_token\":\"yada\",\"token_type\":\"bearer\",\"expires_in\":2592000,\"refresh_token\":\"yada\",\"scope\":\"read\",\"uid\":123456,\"info\":{\"name\":\"first.last\",\"email\":\"first.last@domain.com\"}}";

        private DigitalOceanClientDescendant descendant;
        private IRequestFactory factory;

        [SetUp]
        public void SetUp()
        {
            factory = Substitute.For<IRequestFactory>();
            var client = Substitute.For<IRestClient>();
            var request = Substitute.For<IRestRequest>();
            var response = Substitute.For<IRestResponse>();
            factory.CreateClient().Returns(client);
            factory.CreateRequest(Arg.Any<string>()).ReturnsForAnyArgs(request);
            client.Execute(request).Returns(Task.FromResult(response));
            response.StatusCode.Returns(HttpStatusCode.OK);
            descendant = new DigitalOceanClientDescendant(
                factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://cloud.digitalocean.com");
            endpoint.Resource.Should().Be("/v1/oauth/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://cloud.digitalocean.com");
            endpoint.Resource.Should().Be("/v1/oauth/token");
        }

        [Test]
        [ExpectedException(typeof(NotImplementedException))]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            descendant.GetUserInfoServiceEndpoint();

            // assert
            Assert.Fail();
        }
        
        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = descendant.ParseUserInfo(content);

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