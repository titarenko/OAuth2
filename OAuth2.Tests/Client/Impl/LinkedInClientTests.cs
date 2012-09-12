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
    public class LinkedInClientTests
    {
        private const string Content = "todo";

        private LinkedInClientDescendant descendant;

        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<IRequestFactory>();
            factory.NewClient().Returns(Substitute.For<IRestClient>());
            factory.NewRequest().Returns(Substitute.For<IRestRequest>());

            descendant = new LinkedInClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectRequestTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetRequestTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.linkedin.com");
            endpoint.Resource.Should().Be("/uas/oauth/requestToken");
        }

        [Test]
        public void Should_ReturnCorrectLoginServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetLoginServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/uas/oauth/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.linkedin.com");
            endpoint.Resource.Should().Be("/uas/oauth/accessToken");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("http://api.linkedin.com");
            endpoint.Resource.Should().Be("/v1/people/~:(id,first-name,last-name,picture-url)");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            Assert.Ignore("todo");

            // act
            var info = descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("todo");
            info.FirstName.Should().Be("todo");
            info.LastName.Should().Be("todo");
            info.PhotoUri.Should().Be("todo");
        }

        class LinkedInClientDescendant : LinkedInClient
        {
            public LinkedInClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration)
            {
            }

            public Endpoint GetRequestTokenServiceEndpoint()
            {
                return RequestTokenServiceEndpoint;
            }

            public Endpoint GetLoginServiceEndpoint()
            {
                return LoginServiceEndpoint;
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