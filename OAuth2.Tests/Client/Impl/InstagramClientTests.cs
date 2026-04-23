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
    public class InstagramClientTests
    {
        /* lang=json */
        private const string AccessTokenResponseContent = "{\"access_token\":\"token\",\"user\":{\"id\":\"12345\",\"username\":\"jdoe\",\"full_name\":\"John Doe\",\"profile_picture\":\"https://instagramimages.com/photo.jpg\"}}";

        private InstagramClientDescendant _descendant;
        private IRequestFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _descendant = new InstagramClientDescendant(
                _factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.instagram.com");
            endpoint.Resource.Should().Be("/oauth/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.instagram.com");
            endpoint.Resource.Should().Be("/oauth/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.instagram.com");
            endpoint.Resource.Should().Be("/oauth/access_token");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // arrange
            var response = new RestResponse { Content = AccessTokenResponseContent };
            _descendant.SimulateAfterGetAccessToken(new BeforeAfterRequestArgs { Response = response });

            // act
            var info = _descendant.ParseUserInfo("ignored");

            // assert
            info.Id.Should().Be("12345");
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Doe");
            info.PhotoUri.Should().Be("https://instagramimages.com/photo.jpg");
        }

        private class InstagramClientDescendant : InstagramClient
        {
            public InstagramClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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

            public void SimulateAfterGetAccessToken(BeforeAfterRequestArgs args)
            {
                AfterGetAccessToken(args);
            }
        }
    }
}