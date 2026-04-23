using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2.Tests.TestHelpers;
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
        private MockHttpMessageHandler _handler;
        private List<RestRequest> _capturedRequests;

        [SetUp]
        public void SetUp()
        {
            _handler = new MockHttpMessageHandler();
            _capturedRequests = new List<RestRequest>();

            _factory = Substitute.For<IRequestFactory>();
            _factory.CreateClient(Arg.Any<string>()).Returns(callInfo =>
                new RestClient(new HttpClient(_handler), new RestClientOptions(callInfo.Arg<string>())));
            _factory.CreateRequest(Arg.Any<string>()).Returns(callInfo =>
            {
                var req = new RestRequest(callInfo.Arg<string>());
                _capturedRequests.Add(req);
                return req;
            });
            _factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(callInfo =>
            {
                var req = new RestRequest(callInfo.Arg<string>(), callInfo.Arg<Method>());
                _capturedRequests.Add(req);
                return req;
            });

            var configuration = Substitute.For<IClientConfiguration>();
            configuration.ClientId.Returns("consumer_key");
            configuration.ClientSecret.Returns("consumer_secret");
            configuration.RedirectUri.Returns("http://callback");

            _descendant = new OAuthClientDescendant(_factory, configuration);
        }

        [Test]
        public Task Should_ThrowNotSupported_When_UserWantsToTransmitState()
        {
            return _descendant.Awaiting(x => x.GetLoginLinkUriAsync("any state")).Should().ThrowAsync<NotSupportedException>();
        }

        [Test]
        public Task Should_ThrowUnexpectedResponse_When_StatusIsNotOk()
        {
            _handler.EnqueueResponse(HttpStatusCode.InternalServerError, string.Empty);
            return _descendant.Awaiting(x => x.GetLoginLinkUriAsync()).Should().ThrowAsync<UnexpectedResponseException>();
        }

        [Test]
        public Task Should_ThrowUnexpectedResponse_When_ContentIsEmpty()
        {
            _handler.EnqueueResponse(HttpStatusCode.OK, "");
            return _descendant.Awaiting(x => x.GetLoginLinkUriAsync()).Should().ThrowAsync<UnexpectedResponseException>();
        }

        [Test]
        public Task Should_ThrowUnexpectedResponse_When_OAuthTokenIsEmpty()
        {
            _handler.EnqueueResponse("something=something_other");
            return _descendant
                .Awaiting(x => x.GetLoginLinkUriAsync())
                .Should().ThrowAsync<UnexpectedResponseException>();
        }

        [Test]
        public Task Should_ThrowUnexpectedResponse_When_OAuthSecretIsEmpty()
        {
            _handler.EnqueueResponse("oauth_token=token");
            return _descendant
                .Awaiting(x => x.GetLoginLinkUriAsync())
                .Should().ThrowAsync<UnexpectedResponseException>();
        }

        [Test]
        public async Task Should_IssueCorrectRequestForRequestToken_When_GetLoginLinkUriIsCalled()
        {
            // arrange
            _handler.EnqueueResponse("oauth_token=token&oauth_token_secret=secret");

            // act
            await _descendant.GetLoginLinkUriAsync();

            // assert
            _factory.Received().CreateClient("https://RequestTokenServiceEndpoint");
            _factory.Received().CreateRequest("/RequestTokenServiceEndpoint", Method.Post);

            var requestTokenRequest = _capturedRequests.First(r => r.Method == Method.Post);
            requestTokenRequest.Authenticator.Should().NotBeNull();
            requestTokenRequest.Authenticator.Should().BeOfType<OAuth1Authenticator>();
        }

        [Test]
        public async Task Should_ComposeCorrectLoginUri_When_GetLoginLinkIsCalled()
        {
            // arrange
            _handler.EnqueueResponse("oauth_token=token5&oauth_token_secret=secret");

            // act
            var uri = await _descendant.GetLoginLinkUriAsync();

            // assert
            var parsedUri = new Uri(uri);
            parsedUri.Host.Should().Be("loginserviceendpoint");
            var queryParams = System.Web.HttpUtility.ParseQueryString(parsedUri.Query);
            queryParams["oauth_token"].Should().Be("token5");

            _factory.Received().CreateClient("https://LoginServiceEndpoint");
            _factory.Received().CreateRequest("/LoginServiceEndpoint", Method.Get);
        }

        [Test]
        public async Task Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // arrange
            _handler.EnqueueResponse("oauth_token=token&oauth_token_secret=secret");
            _handler.EnqueueResponse("content");

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                { "oauth_token", "token1" },
                { "oauth_verifier", "verifier100" }
            });

            // assert
            _factory.Received().CreateClient("https://AccessTokenServiceEndpoint");
            _factory.Received().CreateRequest("/AccessTokenServiceEndpoint", Method.Post);

            var accessTokenRequest = _capturedRequests.First(r => r.Method == Method.Post);
            accessTokenRequest.Authenticator.Should().NotBeNull();
            accessTokenRequest.Authenticator.Should().BeOfType<OAuth1Authenticator>();
        }

        [Test]
        public async Task Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled()
        {
            // arrange
            _handler.EnqueueResponse("oauth_token=token&oauth_token_secret=secret");
            _handler.EnqueueResponse("abba");

            // act
            var info = await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                { "oauth_token", "token1" },
                { "oauth_verifier", "verifier100" }
            });

            // assert
            _factory.Received().CreateClient("https://UserInfoServiceEndpoint");
            _factory.Received().CreateRequest("/UserInfoServiceEndpoint", Method.Get);

            var userInfoRequest = _capturedRequests.Last();
            userInfoRequest.Authenticator.Should().NotBeNull();
            userInfoRequest.Authenticator.Should().BeAssignableTo<OAuth1Authenticator>();

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