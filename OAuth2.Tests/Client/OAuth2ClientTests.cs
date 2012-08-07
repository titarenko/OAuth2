using System;
using System.Collections.Specialized;
using FizzWare.NBuilder;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using FluentAssertions;

namespace OAuth2.Tests.Client
{
    [TestFixture]
    public class OAuth2ClientTests
    {
        private OAuth2ClientDescendant descendant;

        private IRequestFactory factory;
        private IRestClient restClient;
        private IRestRequest restRequest;
        private IRestResponse restResponse;

        [SetUp]
        public void SetUp()
        {
            restRequest = Substitute.For<IRestRequest>();
            restResponse = Substitute.For<IRestResponse>();

            restClient = Substitute.For<IRestClient>();
            restClient.Execute(restRequest).Returns(restResponse);

            factory = Substitute.For<IRequestFactory>();
            factory.NewClient().Returns(restClient);
            factory.NewRequest().Returns(restRequest);

            var configuration = Substitute.For<IClientConfiguration>();

            configuration.ClientId.Returns("client_id");
            configuration.ClientSecret.Returns("client_secret");
            configuration.RedirectUri.Returns("http://redirect-uri.net");
            configuration.Scope.Returns("scope");

            descendant = new OAuth2ClientDescendant(factory, configuration);
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeRequestUri()
        {
            // arrange
            restClient.BuildUri(restRequest).Returns(new Uri("https://login-link.net/"));

            // act
            var uri = descendant.GetLoginLinkUri();

            // assert
            uri.Should().Be("https://login-link.net/");

            factory.Received(1).NewClient();
            factory.Received(1).NewRequest();

            restClient.Received(1).BaseUrl = "https://AccessCodeServiceEndpoint";
            restRequest.Received(1).Resource = "/AccessCodeServiceEndpoint";

            restRequest.Received(1).AddObject(Arg.Is<object>(
                x => x.AllPropertiesAreEqualTo(
                    new
                    {
                        response_type = "code",
                        client_id = "client_id",
                        redirect_uri = "http://redirect-uri.net",
                        scope = "scope"
                    })));

            restClient.Received(1).BuildUri(restRequest);
        }
        
        [Test]
        public void Should_ThrowException_WhenParametersForGetUserInfoContainError()
        {
            // arrange
            var parameters = new NameValueCollection {{"error", "error2"}};

            // act & assert
            descendant
                .Invoking(x => x.GetUserInfo(parameters))
                .ShouldThrow<ApplicationException>()
                .WithMessage("error2");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void ShouldNot_ThrowException_When_ParametersForGetUserInfoContainEmptyError(string error)
        {
            // act & assert
            descendant
                .Invoking(x => x.GetUserInfo(new NameValueCollection {{"error", error}}))
                .ShouldNotThrow();
        }

        [Test]
        public void Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // act
            descendant.GetUserInfo(new NameValueCollection {{"code", "code"}});

            // assert
            restClient.Received(1).BaseUrl = "https://AccessTokenServiceEndpoint";
            restRequest.Received(1).Resource = "/AccessTokenServiceEndpoint";
            restRequest.Received(1).Method = Method.POST;
            restRequest.Received(1).AddObject(Arg.Is<object>(x => x.AllPropertiesAreEqualTo(
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
        [TestCase("{\"access_token\": \"token\"")]
        public void Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled(string response)
        {
            // arrange
            restResponse.Content.Returns(response);

            // act
            descendant.GetUserInfo(new NameValueCollection {{"code", "code"}});

            // assert
            restClient.Received(1).BaseUrl = "https://UserInfoServiceEndpoint";
            restRequest.Received(1).Resource = "/UserInfoServiceEndpoint";
            restClient.Authenticator.Should().BeOfType<OAuth2UriQueryParameterAuthenticator>();
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

            protected override UserInfo ParseUserInfo(string content)
            {
                return Builder<UserInfo>.CreateNew()
                    .With(x => x.Id = content)
                    .Build();
            }
        }
    }
}