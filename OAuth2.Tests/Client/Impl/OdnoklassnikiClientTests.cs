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
    public class OdnoklassnikiClientTests
    {
        /* lang=json */
        private const string Content = "{\"uid\":\"12345\",\"first_name\":\"Oleg\",\"last_name\":\"Ivanov\",\"pic_1\":\"https://i.mycdn.me/image?id=123&photoType=4\"}";

        private OdnoklassnikiClientDescendant _descendant;
        private IRequestFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _descendant = new OdnoklassnikiClientDescendant(
                _factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("http://www.odnoklassniki.ru");
            endpoint.Resource.Should().Be("/oauth/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("http://api.odnoklassniki.ru");
            endpoint.Resource.Should().Be("/oauth/token.do");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("http://api.odnoklassniki.ru");
            endpoint.Resource.Should().Be("/fb.do");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("12345");
            info.FirstName.Should().Be("Oleg");
            info.LastName.Should().Be("Ivanov");
            info.AvatarUri.Large.Should().Be("https://i.mycdn.me/image?id=123&photoType=6");
        }

        private class OdnoklassnikiClientDescendant : OdnoklassnikiClient
        {
            public OdnoklassnikiClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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