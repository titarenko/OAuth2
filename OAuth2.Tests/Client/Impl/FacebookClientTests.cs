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
    public class FacebookClientTests
    {
        private const string Content = "{\"email\":\"email\",\"first_name\":\"name\",\"last_name\":\"surname\",\"id\":\"id\",\"picture\":{\"data\":{\"url\":\"picture\"}}}";

        private FacebookClientDescendant _descendant;
        private IRequestFactory _factory;
        private IRestClient _restClient;
        private IRestRequest _restRequest;
        private IRestResponse _restResponse;

        [SetUp]
        public void SetUp()
        {
            _restRequest = Substitute.For<IRestRequest>();
            _restResponse = Substitute.For<IRestResponse>();

            _restResponse.StatusCode.Returns(HttpStatusCode.OK);
            _restResponse.Content.Returns("response");

            _restClient = Substitute.For<IRestClient>();
            _restClient.ExecuteTaskAsync(_restRequest, CancellationToken.None).Returns(_restResponse);

            _factory = Substitute.For<IRequestFactory>();
            _factory.CreateClient().Returns(_restClient);
            _factory.CreateRequest().Returns(_restRequest);

            _descendant = new FacebookClientDescendant(_factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.facebook.com");
            endpoint.Resource.Should().Be("/dialog/oauth");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://graph.facebook.com");
            endpoint.Resource.Should().Be("/oauth/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://graph.facebook.com");
            endpoint.Resource.Should().Be("/me");
        }
        
        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = _descendant.ParseUserInfo(Content);

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
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None))
                .Content.Returns(
                    "any content to pass response verification",
                    "access_token=token",
                    Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                {"code", "code"}
            });

            // assert
            _factory.CreateRequest()
                .Received(1)
                .AddParameter(Arg.Is("fields"), Arg.Is("id,first_name,last_name,email,picture"));
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