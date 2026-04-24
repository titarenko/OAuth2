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
    public class GoogleClientTests
    {
        /* lang=json */
        private const string Content = "{\"email\":\"email\",\"given_name\":\"name\",\"family_name\":\"surname\",\"sub\":\"id\"}";
        /* lang=json */
        private const string ContentWithPicture = "{\"email\":\"email\",\"given_name\":\"name\",\"family_name\":\"surname\",\"sub\":\"id\",\"picture\":\"picture\"}";

        private GoogleClientDescendant _descendant = null!;

        [SetUp]
        public void SetUp()
        {
            _descendant = new GoogleClientDescendant(Substitute.For<IRequestFactory>(), Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://accounts.google.com");
            endpoint.Resource.Should().Be("/o/oauth2/v2/auth");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth2.googleapis.com");
            endpoint.Resource.Should().Be("/token");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.googleapis.com");
            endpoint.Resource.Should().Be("/oauth2/v3/userinfo");
        }

        [Test]
        public void ParseUserInfo_NoPicture_DoesNotThrow()
        {
            // arrange (uses Content const without picture)

            // act & assert
            _descendant.Invoking(x => x.ParseUserInfo(Content)).Should().NotThrow();
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses ContentWithPicture const)

            // act
            var info = _descendant.ParseUserInfo(ContentWithPicture);

            // assert
            info.Id.Should().Be("id");
            info.FirstName.Should().Be("name");
            info.LastName.Should().Be("surname");
            info.Email.Should().Be("email");
            info.PhotoUri.Should().Be("picture");
        }

        class GoogleClientDescendant : GoogleClient
        {
            public GoogleClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
