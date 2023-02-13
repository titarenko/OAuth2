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
    public class OAuth2ClientTests
    {
        private OAuth2ClientDescendant _descendant;

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
            _restClient.ExecuteAsync(_restRequest, CancellationToken.None).Returns(_restResponse);

            _factory = Substitute.For<IRequestFactory>();
            _factory.CreateClient().Returns(_restClient);
            _factory.CreateRequest().Returns(_restRequest);

            var configuration = Substitute.For<IClientConfiguration>();

            configuration.ClientId.Returns("client_id");
            configuration.ClientSecret.Returns("client_secret");
            configuration.RedirectUri.Returns("http://redirect-uri.net");
            configuration.Scope.Returns("scope");

            _descendant = new OAuth2ClientDescendant(_factory, configuration);
        }

        [Test]
        public void Should_ThrowUnexpectedResponse_When_CodeIsNotOk()
        {
            _restResponse.StatusCode = HttpStatusCode.InternalServerError;

            _descendant
                .Awaiting(x => x.GetUserInfoAsync(new NameValueCollection()))
                .Should().Throw<UnexpectedResponseException>();
        }

        [Test]
        public void Should_ThrowUnexpectedResponse_When_ResponseIsEmpty()
        {
            _restResponse.StatusCode = HttpStatusCode.OK;
            _restResponse.Content.Returns("");
            
            _descendant
                .Awaiting(x => x.GetUserInfoAsync(new NameValueCollection()))
                .Should().Throw<UnexpectedResponseException>();
        }

        [Test]
        public async Task Should_ReturnCorrectAccessCodeRequestUri()
        {
            // arrange
            _restClient.BuildUri(_restRequest).Returns(new Uri("https://login-link.net/"));

            // act
            var uri = await _descendant.GetLoginLinkUriAsync();

            // assert
            uri.Should().Be("https://login-link.net/");

            _factory.Received(1).CreateClient();
            _factory.Received(1).CreateRequest();

            _restClient.Received(1).BaseUrl = new Uri("https://AccessCodeServiceEndpoint");
            _restRequest.Received(1).Resource = "/AccessCodeServiceEndpoint";

            _restRequest.Received(1).AddObject(Arg.Is<object>(
                x => x.AllPropertiesAreEqualTo(
                    new
                    {
                        response_type = "code",
                        client_id = "client_id",
                        redirect_uri = "http://redirect-uri.net",
                        scope = "scope",
                        state = (string) null
                    })));

            _restClient.Received(1).BuildUri(_restRequest);
        }
        
        [Test]
        public void Should_ThrowException_WhenParametersForGetUserInfoContainError()
        {
            // arrange
            var parameters = new NameValueCollection {{"error", "error2"}};

            // act & assert
            _descendant
                .Awaiting(x => x.GetUserInfoAsync(parameters))
                .Should().Throw<UnexpectedResponseException>()
                .And.FieldName.Should().Be("error");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void ShouldNot_ThrowException_When_ParametersForGetUserInfoContainEmptyError(string error)
        {
            // arrange
            _restResponse.Content.Returns("access_token=token");

            // act & assert
            _descendant
                .Awaiting(x => x.GetUserInfoAsync(new NameValueCollection
                {
                    {"error", error},
                    {"code", "code"}
                }))
                .Should().NotThrow();
        }

        [Test]
        public async Task Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // arrange
            _restResponse.Content = "access_token=token";

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection {{"code", "code"}});

            // assert
            _restClient.Received(1).BaseUrl = new Uri("https://AccessTokenServiceEndpoint");
            _restRequest.Received(1).Resource = "/AccessTokenServiceEndpoint";
            _restRequest.Received(1).Method = Method.POST;
            _restRequest.Received(1).AddObject(Arg.Is<object>(x => x.AllPropertiesAreEqualTo(
                new
                {
                    code = "code",
                    client_id = "client_id",
                    client_secret = "client_secret",
                    redirect_uri = "http://redirect-uri.net",
                    grant_type = "authorization_code"
                })));
        }

        [Test]
        [TestCase("access_token=token")]
        [TestCase("{\"access_token\": \"token\"}")]
        public async Task Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled(string response)
        {
            // arrange
            _restResponse.Content.Returns(response);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection { { "code", "code" } });

            // assert
            _restClient.Received(1).BaseUrl = new Uri("https://UserInfoServiceEndpoint");
            _restRequest.Received(1).Resource = "/UserInfoServiceEndpoint";
            _restClient.Authenticator.Should().BeOfType<OAuth2UriQueryParameterAuthenticator>();
        }

        [Test]
        public async Task Should_Update_RefreshToken_When_New_Token_Is_Provided_in_GetCurrentTokenAsync()
        {
            // arrange
            var oldRefreshToken = "old-refresh-token";
            var newRefreshToken = "new-refresh-token";
            var response = @$"{{""access_token"": ""abc123"", ""refresh_token"": ""{newRefreshToken}""}}";
            _restResponse.Content.Returns(response);

            // act
            await _descendant.GetCurrentTokenAsync(oldRefreshToken);

            // assert
            Assert.That(_descendant.RefreshToken, Is.EqualTo(newRefreshToken));
        }

        [Test]
        public async Task Should_Not_Modify_RefreshToken_When_Not_Included_In_Response_From_GetCurrentTokenAsync()
        {
            // arrange
            var currentRefreshToken = "refresh-token";
            var initialTokenResponse = @$"{{""access_token"": ""abc123"", ""refresh_token"": ""{currentRefreshToken}""}}";
            var refreshTokenResponse = @"{""access_token"": ""abc123""}";
            
            // simulate getting the initial token (to populate refresh token)
            _restResponse.Content.Returns(initialTokenResponse);
            await _descendant.GetTokenAsync(new NameValueCollection {{"code", "auth-code"}});
            Assert.That(_descendant.RefreshToken, Is.EqualTo(currentRefreshToken));

            // setup response for refresh token request
            _restResponse.Content.Returns(refreshTokenResponse);

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