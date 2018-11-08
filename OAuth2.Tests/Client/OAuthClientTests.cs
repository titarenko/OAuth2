using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using FluentAssertions;
using RestSharp.Authenticators;

namespace OAuth2.Tests.Client
{
    [TestFixture]
    public class OAuthClientTests
    {
        private OAuthClientDescendant _descendant;

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

            _descendant = new OAuthClientDescendant(_factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ThrowNotSupported_When_UserWantsToTransmitState()
        {
            _descendant.Awaiting(x => x.GetLoginLinkUriAsync("any state")).Should().Throw<NotSupportedException>();
        }

        [Test]
        public async Task Should_ThrowUnexpectedResponse_When_StatusIsNotOk()
        {
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).StatusCode = HttpStatusCode.InternalServerError;
            await _descendant.Awaiting(x => x.GetLoginLinkUriAsync()).Should().ThrowAsync<UnexpectedResponseException>();
        }

        [Test]
        public async Task Should_ThrowUnexpectedResponse_When_ContentIsEmpty()
        {
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).Content = "";
            await _descendant.Awaiting(x => x.GetLoginLinkUriAsync()).Should().ThrowAsync<UnexpectedResponseException>();
        }

        [Test]
        public async Task Should_ThrowUnexpectedResponse_When_OAuthTokenIsEmpty()
        {
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).Content = "something=something_other";
            (await _descendant
                .Awaiting(x => x.GetLoginLinkUriAsync())
                .Should().ThrowAsync<UnexpectedResponseException>())
                .And.FieldName.Should().Be("oauth_token");
        }

        [Test]
        public async Task Should_ThrowUnexpectedResponse_When_OAuthSecretIsEmpty()
        {
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).Content = "oauth_token=token";
            (await _descendant
                .Awaiting(x => x.GetLoginLinkUriAsync())
                .Should().ThrowAsync<UnexpectedResponseException>())
                .And.FieldName.Should().Be("oauth_token_secret");
        }

        [Test]
        public async Task Should_IssueCorrectRequestForRequestToken_When_GetLoginLinkUriIsCalled()
        {
            // arrange
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            restClient.BuildUri(restRequest).Returns(new Uri("http://login"));
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).Content = "oauth_token=token&oauth_token_secret=secret";

            // act
            await _descendant.GetLoginLinkUriAsync();

            // assert
            _factory.Received().CreateClient();
            _factory.Received().CreateRequest();

            restClient.Received().BaseUrl = new Uri("https://RequestTokenServiceEndpoint");
            restRequest.Received().Resource = "/RequestTokenServiceEndpoint";
            restRequest.Received().Method = Method.POST;

            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeOfType<OAuth1Authenticator>();
        }

        [Test]
        public async Task Should_ComposeCorrectLoginUri_When_GetLoginLinkIsCalled()
        {
            // arrange
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            restClient.BuildUri(restRequest).Returns(new Uri("https://login/"));
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).Content.Returns("oauth_token=token5&oauth_token_secret=secret");

            // act
            var uri = await _descendant.GetLoginLinkUriAsync();

            // assert
            uri.Should().Be("https://login/");

            _factory.Received().CreateClient();
            _factory.Received().CreateRequest();
            
            restClient.Received().BaseUrl = new Uri("https://LoginServiceEndpoint");
            restRequest.Received().Resource = "/LoginServiceEndpoint";
            restRequest.Received().AddParameter("oauth_token", "token5");
        }

        [Test]
        public async Task Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // arrange
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).Content = "oauth_token=token&oauth_token_secret=secret";
            
            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                {"oauth_token", "token1"},
                {"oauth_verifier", "verifier100"}
            });

            // assert
            _factory.Received().CreateClient();
            _factory.Received().CreateRequest();

            restClient.Received().BaseUrl = new Uri("https://AccessTokenServiceEndpoint");
            restRequest.Received().Resource = "/AccessTokenServiceEndpoint";
            restRequest.Received().Method = Method.POST;
            
            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeOfType<OAuth1Authenticator>();
        }

        [Test]
        public async Task Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled()
        {
            // arrange
            var restClient = _factory.CreateClient();
            var restRequest = _factory.CreateRequest();
            (await restClient.ExecuteTaskAsync(restRequest, CancellationToken.None)).Content.Returns(
                "something to pass response verification", 
                "oauth_token=token&oauth_token_secret=secret", 
                "abba");

            // act
            var info = await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                {"oauth_token", "token1"},
                {"oauth_verifier", "verifier100"}
            });

            // assert
            _factory.Received().CreateClient();
            _factory.Received().CreateRequest();

            restClient.Received().BaseUrl = new Uri("https://UserInfoServiceEndpoint");
            restRequest.Received().Resource = "/UserInfoServiceEndpoint";

            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeAssignableTo<OAuth1Authenticator>();

            info.Id.Should().Be("abba");
        }

        class OAuthClientDescendant : OAuthClient
        {
            public OAuthClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration)
            {
            }
            
            protected override Endpoint RequestTokenServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://RequestTokenServiceEndpoint",
                        Resource = "/RequestTokenServiceEndpoint"
                    };
                }
            }

            protected override Endpoint LoginServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://LoginServiceEndpoint",
                        Resource = "/LoginServiceEndpoint"
                    };
                }
            }

            protected override Endpoint AccessTokenServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://AccessTokenServiceEndpoint",
                        Resource = "/AccessTokenServiceEndpoint"
                    };
                }
            }

            protected override Endpoint UserInfoServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://UserInfoServiceEndpoint",
                        Resource = "/UserInfoServiceEndpoint"
                    };
                }
            }

            public override string Name
            {
                get { return "OAuthClientTest"; }
            }

            protected override UserInfo ParseUserInfo(string content)
            {
                return Builder<UserInfo>.CreateNew()
                    .With(x => x.Id = content)
                    .Build();
            }
        }
    }

}