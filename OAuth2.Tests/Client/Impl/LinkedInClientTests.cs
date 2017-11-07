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
        private const string Content = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                                        "<person>" +
                                        "  <id>id</id>" +
                                        "  <first-name>firstname</first-name>" +
                                        "  <last-name>lastname</last-name>" +
                                        "  <picture-url>pictureurl</picture-url>" +
                                        "</person>";

        private LinkedInClientDescendant descendant;

        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient().Returns(Substitute.For<IRestClient>());
            factory.CreateRequest().Returns(Substitute.For<IRestRequest>());

            descendant = new LinkedInClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/uas/oauth2/authorization");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.linkedin.com");
            endpoint.Resource.Should().Be("/uas/oauth2/accessToken");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.linkedin.com");
            endpoint.Resource.Should().Be("/v1/people/~:(id,email-address,first-name,last-name,picture-url)");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("id");
            info.FirstName.Should().Be("firstname");
            info.LastName.Should().Be("lastname");
            info.PhotoUri.Should().Be("pictureurl");
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
