using System;
using System.Collections.Specialized;
using System.Net;
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
        private IRequestFactory factory;
        private OAuthClientDescendant descendant;

        [SetUp]
        public void SetUp()
        {
            factory = Substitute.For<IRequestFactory>();
            factory.CreateClient().Execute(factory.CreateRequest()).StatusCode = HttpStatusCode.OK;
            descendant = new OAuthClientDescendant(
                factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ThrowNotSupported_When_UserWantsToTransmitState()
        {
            descendant.Invoking(x => x.GetLoginLinkUri("any state")).ShouldThrow<NotSupportedException>();
        }

        [Test]
        public void Should_ThrowUnexpectedResponse_When_StatusIsNotOk()
        {
            factory.CreateClient().Execute(factory.CreateRequest()).StatusCode = HttpStatusCode.InternalServerError;
            descendant.Invoking(x => x.GetLoginLinkUri()).ShouldThrow<UnexpectedResponseException>();
        }

        [Test]
        public void Should_ThrowUnexpectedResponse_When_ContentIsEmpty()
        {
            factory.CreateClient().Execute(factory.CreateRequest()).Content = "";
            descendant.Invoking(x => x.GetLoginLinkUri()).ShouldThrow<UnexpectedResponseException>();
        }

        [Test]
        public void Should_ThrowUnexpectedResponse_When_OAuthTokenIsEmpty()
        {
            factory.CreateClient().Execute(factory.CreateRequest()).Content = "something=something_other";
            descendant
                .Invoking(x => x.GetLoginLinkUri())
                .ShouldThrow<UnexpectedResponseException>()
                .And.FieldName.Should().Be("oauth_token");
        }

        [Test]
        public void Should_ThrowUnexpectedResponse_When_OAuthSecretIsEmpty()
        {
            factory.CreateClient().Execute(factory.CreateRequest()).Content = "oauth_token=token";
            descendant
                .Invoking(x => x.GetLoginLinkUri())
                .ShouldThrow<UnexpectedResponseException>()
                .And.FieldName.Should().Be("oauth_token_secret");
        }

        [Test]
        public void Should_IssueCorrectRequestForRequestToken_When_GetLoginLinkUriIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest();
            restClient.BuildUri(restRequest).Returns(new Uri("http://login"));
            restClient.Execute(restRequest).Content = "oauth_token=token&oauth_token_secret=secret";

            // act
            descendant.GetLoginLinkUri();

            // assert
            factory.Received().CreateClient();
            factory.Received().CreateRequest();

            restClient.Received().BaseUrl = "https://RequestTokenServiceEndpoint";
            restRequest.Received().Resource = "/RequestTokenServiceEndpoint";
            restRequest.Received().Method = Method.POST;

            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeOfType<OAuth1Authenticator>();
        }

        [Test]
        public void Should_ComposeCorrectLoginUri_When_GetLoginLinkIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest();
            restClient.BuildUri(restRequest).Returns(new Uri("https://login/"));
            restClient.Execute(restRequest).Content.Returns("oauth_token=token5&oauth_token_secret=secret");

            // act
            var uri = descendant.GetLoginLinkUri();

            // assert
            uri.Should().Be("https://login/");

            factory.Received().CreateClient();
            factory.Received().CreateRequest();
            
            restClient.Received().BaseUrl = "https://LoginServiceEndpoint";
            restRequest.Received().Resource = "/LoginServiceEndpoint";
            restRequest.Received().AddParameter("oauth_token", "token5");
        }

        [Test]
        public void Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest();
            restClient.Execute(restRequest).Content = "oauth_token=token&oauth_token_secret=secret";
            
            // act
            descendant.GetUserInfo(new NameValueCollection
            {
                {"oauth_token", "token1"},
                {"oauth_verifier", "verifier100"}
            });

            // assert
            factory.Received().CreateClient();
            factory.Received().CreateRequest();

            restClient.Received().BaseUrl = "https://AccessTokenServiceEndpoint";
            restRequest.Received().Resource = "/AccessTokenServiceEndpoint";
            restRequest.Received().Method = Method.POST;
            
            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeOfType<OAuth1Authenticator>();
        }

        [Test]
        public void Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled()
        {
            // arrange
            var restClient = factory.CreateClient();
            var restRequest = factory.CreateRequest();
            restClient.Execute(restRequest).Content.Returns(
                "something to pass response verification", 
                "oauth_token=token&oauth_token_secret=secret", 
                "abba");

            // act
            var info = descendant.GetUserInfo(new NameValueCollection
            {
                {"oauth_token", "token1"},
                {"oauth_verifier", "verifier100"}
            });

            // assert
            factory.Received().CreateClient();
            factory.Received().CreateRequest();

            restClient.Received().BaseUrl = "https://UserInfoServiceEndpoint";
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