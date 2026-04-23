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
using RestSharp.Authenticators.OAuth2;

namespace OAuth2.Tests.Client
{
    [TestFixture]
    public class OAuth2ClientTests
    {
        private OAuth2ClientDescendant _descendant;
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
            configuration.ClientId.Returns("client_id");
            configuration.ClientSecret.Returns("client_secret");
            configuration.RedirectUri.Returns("http://redirect-uri.net");
            configuration.Scope.Returns("scope");

            _descendant = new OAuth2ClientDescendant(_factory, configuration);
        }

        [Test]
        public Task Should_ThrowUnexpectedResponse_When_CodeIsNotOk()
        {
            _handler.EnqueueResponse(HttpStatusCode.InternalServerError, string.Empty);

            return _descendant
                .Awaiting(x => x.GetUserInfoAsync(new NameValueCollection { { "code", "code" } }))
                .Should().ThrowAsync<UnexpectedResponseException>();
        }

        [Test]
        public Task Should_ThrowUnexpectedResponse_When_ResponseIsEmpty()
        {
            _handler.EnqueueResponse(HttpStatusCode.OK, "");

            return _descendant
                .Awaiting(x => x.GetUserInfoAsync(new NameValueCollection { { "code", "code" } }))
                .Should().ThrowAsync<UnexpectedResponseException>();
        }

        [Test]
        public async Task Should_ReturnCorrectAccessCodeRequestUri()
        {
            // act
            var uri = await _descendant.GetLoginLinkUriAsync();

            // assert
            var parsedUri = new Uri(uri);
            parsedUri.Scheme.Should().Be("https");
            parsedUri.Host.Should().Be("accesscodeserviceendpoint");
            parsedUri.AbsolutePath.Should().Be("/AccessCodeServiceEndpoint");

            var queryParams = System.Web.HttpUtility.ParseQueryString(parsedUri.Query);
            queryParams["response_type"].Should().Be("code");
            queryParams["client_id"].Should().Be("client_id");
            queryParams["redirect_uri"].Should().Be("http://redirect-uri.net");
            queryParams["scope"].Should().Be("scope");

            _factory.Received(1).CreateClient("https://AccessCodeServiceEndpoint");
            _factory.Received(1).CreateRequest("/AccessCodeServiceEndpoint", Method.Get);
        }

        [Test]
        public async Task Should_ThrowException_WhenParametersForGetUserInfoContainError()
        {
            // arrange
            var parameters = new NameValueCollection { { "error", "error2" } };

            // act & assert
            var ex = await _descendant
                .Awaiting(x => x.GetUserInfoAsync(parameters))
                .Should().ThrowAsync<UnexpectedResponseException>();
            ex.And.FieldName.Should().Be("error");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public Task ShouldNot_ThrowException_When_ParametersForGetUserInfoContainEmptyError(string error)
        {
            // arrange
            _handler.EnqueueResponse("access_token=token");
            _handler.EnqueueResponse("content");

            // act & assert
            return _descendant
                .Awaiting(x => x.GetUserInfoAsync(new NameValueCollection
                {
                    { "error", error },
                    { "code", "code" }
                }))
                .Should().NotThrowAsync();
        }

        [Test]
        public async Task Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // arrange
            _handler.EnqueueResponse("access_token=token");
            _handler.EnqueueResponse("content");

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection { { "code", "code" } });

            // assert
            _factory.Received().CreateClient("https://AccessTokenServiceEndpoint");
            _factory.Received().CreateRequest("/AccessTokenServiceEndpoint", Method.Post);

            var accessTokenRequest = _capturedRequests.First(r => r.Method == Method.Post);
            var parameters = accessTokenRequest.Parameters;
            parameters.FirstOrDefault(p => p.Name == "code")?.Value.Should().Be("code");
            parameters.FirstOrDefault(p => p.Name == "client_id")?.Value.Should().Be("client_id");
            parameters.FirstOrDefault(p => p.Name == "client_secret")?.Value.Should().Be("client_secret");
            parameters.FirstOrDefault(p => p.Name == "redirect_uri")?.Value.Should().Be("http://redirect-uri.net");
            parameters.FirstOrDefault(p => p.Name == "grant_type")?.Value.Should().Be("authorization_code");
        }

        [Test]
        [TestCase("access_token=token")]
        [TestCase("{\"access_token\": \"token\"}")]
        public async Task Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled(string response)
        {
            // arrange
            _handler.EnqueueResponse(response);
            _handler.EnqueueResponse("content");

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection { { "code", "code" } });

            // assert
            _factory.Received().CreateClient("https://UserInfoServiceEndpoint");
            _factory.Received().CreateRequest("/UserInfoServiceEndpoint", Method.Get);

            var userInfoRequest = _capturedRequests.Last();
            userInfoRequest.Authenticator.Should().BeOfType<OAuth2UriQueryParameterAuthenticator>();
        }

        [Test]
        public async Task Should_Update_RefreshToken_When_New_Token_Is_Provided_in_GetCurrentTokenAsync()
        {
            // arrange
            var newRefreshToken = "new-refresh-token";
            _handler.EnqueueResponse($@"{{""access_token"": ""abc123"", ""refresh_token"": ""{newRefreshToken}""}}");

            // act
            await _descendant.GetCurrentTokenAsync("old-refresh-token");

            // assert
            Assert.That(_descendant.RefreshToken, Is.EqualTo(newRefreshToken));
        }

        [Test]
        public async Task Should_Not_Modify_RefreshToken_When_Not_Included_In_Response_From_GetCurrentTokenAsync()
        {
            // arrange
            var currentRefreshToken = "refresh-token";
            var initialTokenResponse = $@"{{""access_token"": ""abc123"", ""refresh_token"": ""{currentRefreshToken}""}}";
            var refreshTokenResponse = @"{""access_token"": ""abc123""}";

            // simulate getting the initial token (to populate refresh token)
            _handler.EnqueueResponse(initialTokenResponse);
            await _descendant.GetTokenAsync(new NameValueCollection { { "code", "auth-code" } });
            Assert.That(_descendant.RefreshToken, Is.EqualTo(currentRefreshToken));

            // setup response for refresh token request
            _handler.EnqueueResponse(refreshTokenResponse);

            // act
            await _descendant.GetCurrentTokenAsync(currentRefreshToken);

            // assert
            Assert.That(_descendant.RefreshToken, Is.EqualTo(currentRefreshToken));
        }

        class OAuth2ClientDescendant : OAuth2Client
        {
            public OAuth2ClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration)
            {
            }

            protected override Endpoint AccessCodeServiceEndpoint
            {
                get
                {
                    return new Endpoint
                    {
                        BaseUri = "https://AccessCodeServiceEndpoint",
                        Resource = "/AccessCodeServiceEndpoint"
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
                get { return "OAuth2ClientTest"; }
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