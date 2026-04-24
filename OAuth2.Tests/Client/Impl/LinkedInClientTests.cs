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
        /* lang=json */
        private const string Content = @"{""sub"":""id"",""given_name"":""firstname"",""family_name"":""lastname"",""email"":""user@linkedin.com"",""picture"":""https://media.licdn.com/photo.jpg""}";

        private LinkedInClientDescendant _descendant = null!;

        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(callInfo =>
                new RestClient(new RestClientOptions(callInfo.Arg<string>())));
            factory.CreateRequest(Arg.Any<string>()).Returns(callInfo =>
                new RestRequest(callInfo.Arg<string>()));
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(callInfo =>
                new RestRequest(callInfo.Arg<string>(), callInfo.Arg<Method>()));

            _descendant = new LinkedInClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/oauth/v2/authorization");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/oauth/v2/accessToken");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.linkedin.com");
            endpoint.Resource.Should().Be("/v2/userinfo");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("id");
            info.FirstName.Should().Be("firstname");
            info.LastName.Should().Be("lastname");
            info.Email.Should().Be("user@linkedin.com");
            info.AvatarUri.Normal.Should().Be("https://media.licdn.com/photo.jpg");
        }

        class LinkedInClientDescendant : LinkedInClient
        {
            public LinkedInClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
