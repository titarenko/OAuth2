using System.Linq;
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
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp.Portable;

namespace OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class FacebookClientTests
    {
        private const string content = "{\"email\":\"email\",\"first_name\":\"name\",\"last_name\":\"surname\",\"id\":\"id\",\"picture\":{\"data\":{\"url\":\"picture\"}}}";

        private FacebookClientDescendant descendant;
        private IRequestFactory requestFactory;

        private static System.Text.Encoding _encoding = System.Text.Encoding.UTF8;

        [SetUp]
        public void SetUp()
        {
            requestFactory = Substitute.For<IRequestFactory>();
            var client = Substitute.For<IRestClient>();
            var request = Substitute.For<IRestRequest>();
            var response = Substitute.For<IRestResponse>();
            requestFactory.CreateClient().Returns(client);
            requestFactory.CreateRequest(null).ReturnsForAnyArgs(request);
            client.Execute(request).Returns(Task.FromResult(response));
            response.StatusCode.Returns(HttpStatusCode.OK);
            descendant = new FacebookClientDescendant(
                requestFactory, Substitute.For<IClientConfiguration>());
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
            info.PhotoUri.Should().StartWith("picture");
        }

        [Test]
        public async Task Should_AddExtraParameters_WhenOnGetUserInfoIsCalled()
        {
            // arrange
            var response = await requestFactory.CreateClient().Execute(requestFactory.CreateRequest(null));
            response
                .RawBytes.Returns(
                    _encoding.GetBytes("any content to pass response verification"),
                    _encoding.GetBytes("access_token=token"),
                    _encoding.GetBytes(content));

            // act
            await descendant.GetUserInfo(new Dictionary<string, string>
            {
                {"code", "code"}
            }.ToLookup(y => y.Key, y => y.Value));

            // assert
            requestFactory.CreateRequest(null)
                .Parameters.Received(1)
                .Add(Arg.Is<Parameter>(x => x.Name == "fields" && (string)x.Value == "id,first_name,last_name,email,picture"));
        }

        class FacebookClientDescendant : FacebookClient
        {
            public FacebookClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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