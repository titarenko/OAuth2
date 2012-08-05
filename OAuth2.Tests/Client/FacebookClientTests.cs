using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2.Parameters;
using RestSharp;

namespace OAuth2.Tests.Client
{
    [TestFixture]
    public class FacebookClientTests
    {
        private const string content = "{\"email\":\"email\",\"first_name\":\"name\",\"last_name\":\"surname\",\"id\":\"id\",\"picture\":{\"data\":{\"url\":\"picture\"}}}";

        private FacebookClientDescendant descendant;

        [SetUp]
        public void SetUp()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration.GetSection(Arg.Any<Type>()).Returns(configuration);
            configuration.Get<AccessTokenRequestParameters>().Returns(new AccessTokenRequestParameters());
            descendant = new FacebookClientDescendant(null, null, configuration);
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.facebook.com");
            endpoint.Resource.Should().Be("/dialog/oauth");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://graph.facebook.com");
            endpoint.Resource.Should().Be("/oauth/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://graph.facebook.com");
            endpoint.Resource.Should().Be("/me");
        }
        
        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = descendant.ParseUserInfo(content);

            //  assert
            info.Id.Should().Be("id");
            info.FirstName.Should().Be("name");
            info.LastName.Should().Be("surname");
            info.Email.Should().Be("email");
            info.PhotoUri.Should().Be("picture");
        }

        [Test]
        public void Should_AddExtraParameters_WhenOnGetUserInfoIsCalled()
        {
            // arrange
            var request = Substitute.For<IRestRequest>();
            request.Parameters.Returns(new List<Parameter>());

            var response = Substitute.For<IRestResponse>();
            response.Content.Returns(content);

            var client = Substitute.For<IRestClient>();
            client.Execute(Arg.Is(request)).Returns(response);

            var configuration = Substitute.For<IConfiguration>();
            configuration.GetSection(Arg.Any<Type>()).Returns(configuration);
            configuration.Get<AccessTokenRequestParameters>().Returns(new AccessTokenRequestParameters());
            var descendant = new FacebookClientDescendant(client, request, configuration);

            // act
            descendant.GetUserInfo(new NameValueCollection());

            // assert
            request.Received(1).AddParameter(Arg.Is("fields"), Arg.Is("id,first_name,last_name,email,picture"));
        }

        class FacebookClientDescendant : FacebookClient
        {
            public FacebookClientDescendant(IRestClient client, IRestRequest request, IConfiguration configuration)
                : base(client, request, configuration)
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