#pragma warning disable CS0618 // Foursquare v2 OAuth is deprecated
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
    public class FoursquareClientTests
    {
        /* lang=json */
        private const string Content = "{\"response\":{\"user\":{\"id\":\"12345\",\"firstName\":\"Jane\",\"lastName\":\"Smith\",\"contact\":{\"email\":\"jane@example.com\"},\"photo\":{\"prefix\":\"https://img.4sqi.net/\",\"suffix\":\"/photo.jpg\"}}}}";

        private FoursquareClientDescendant _descendant;
        private IRequestFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _descendant = new FoursquareClientDescendant(
                _factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://foursquare.com");
            endpoint.Resource.Should().Be("/oauth2/authorize");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://foursquare.com");
            endpoint.Resource.Should().Be("/oauth2/access_token");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.foursquare.com");
            endpoint.Resource.Should().Be("/v2/users/self");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("12345");
            info.FirstName.Should().Be("Jane");
            info.LastName.Should().Be("Smith");
            info.Email.Should().Be("jane@example.com");
        }

        private class FoursquareClientDescendant : FoursquareClient
        {
            public FoursquareClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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