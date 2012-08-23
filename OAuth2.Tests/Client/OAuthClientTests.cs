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
            descendant = new OAuthClientDescendant(
                factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_IssueCorrectRequestForRequestToken_When_GetLoginLinkUriIsCalled()
        {
            // arrange
            var restClient = factory.NewClient();
            var restRequest = factory.NewRequest();
            restClient.BuildUri(restRequest).Returns(new Uri("http://login"));

            // act
            descendant.GetLoginLinkUri();

            // assert
            factory.Received().NewClient();
            factory.Received().NewRequest();

            restClient.Received().BaseUrl = "https://RequestTokenServiceEndpoint";
            restRequest.Received().Resource = "/RequestTokenServiceEndpoint";
            restRequest.Received().Method = Method.POST;

            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeAssignableTo<OAuth1Authenticator>();
        }

        [Test]
        public void Should_ComposeCorrectLoginUri_When_GetLoginLinkIsCalled()
        {
            // arrange
            var restClient = factory.NewClient();
            var restRequest = factory.NewRequest();
            restClient.BuildUri(restRequest).Returns(new Uri("https://login/"));
            restClient.Execute(restRequest).Content.Returns("oauth_token=token5");

            // act
            var uri = descendant.GetLoginLinkUri();

            // assert
            uri.Should().Be("https://login/");

            factory.Received().NewClient();
            factory.Received().NewRequest();
            
            restClient.Received().BaseUrl = "https://LoginServiceEndpoint";
            restRequest.Received().Resource = "/LoginServiceEndpoint";
            restRequest.Received().AddParameter("oauth_token", "token5");
        }

        [Test]
        public void Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // arrange
            var restClient = factory.NewClient();
            var restRequest = factory.NewRequest();
            
            // act
            descendant.GetUserInfo(new NameValueCollection
            {
                {"oauth_token", "token1"},
                {"verifier", "verifier100"}
            });

            // assert
            factory.Received().NewClient();
            factory.Received().NewRequest();

            restClient.Received().BaseUrl = "https://AccessTokenServiceEndpoint";
            restRequest.Received().Resource = "/AccessTokenServiceEndpoint";
            restRequest.Received().Method = Method.POST;
            
            restClient.Authenticator.Should().NotBeNull();
            restClient.Authenticator.Should().BeAssignableTo<OAuth1Authenticator>();
        }

        [Test]
        public void Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled()
        {
            // arrange
            var restClient = factory.NewClient();
            var restRequest = factory.NewRequest();
            restClient.Execute(restRequest).Content.Returns("abba");

            // act
            var info = descendant.GetUserInfo(new NameValueCollection());

            // assert
            factory.Received().NewClient();
            factory.Received().NewRequest();

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

            public override string ProviderName
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