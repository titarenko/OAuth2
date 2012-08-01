using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2.Parameters;
using RestSharp;
using FluentAssertions;

namespace OAuth2.Tests.Client
{
    [TestFixture]
    public class OAuth2ClientTests
    {
        private IRestClient client;
        private IRestRequest request;
        private IConfiguration config;
        private IRestResponse response;
        private OAuth2ClientDescendant descendant;

        [SetUp]
        public void SetUp()
        {
            request = Substitute.For<IRestRequest>();
            request.Parameters.Returns(new List<Parameter>
            {
                new Parameter {Name = "param1", Type = ParameterType.GetOrPost, Value = "value1"}
            });

            response = Substitute.For<IRestResponse>();
            response.Content.Returns("access_token=token");

            client = Substitute.For<IRestClient>();
            client.Execute(Arg.Is(request)).Returns(response);

            config = Substitute.For<IConfiguration>();
            config.GetSection(Arg.Any<Type>(), Arg.Any<bool>()).Returns(config);
            config.Get<AccessCodeRequestParameters>().Returns(new AccessCodeRequestParameters
            {
                ClientId = "id",
                RedirectUri = "uri",
                Scope = "scope",
                State = "state"
            });
            config.Get<AccessTokenRequestParameters>().Returns(new AccessTokenRequestParameters
            {
                ClientId = "id",
                RedirectUri = "uri",
                ClientSecret = "secret",
                Code = null
            });

            descendant = new OAuth2ClientDescendant(client, request, config);
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeRequestUri()
        {
            // act
            var uri = descendant.GetAccessCodeRequestUri();

            // assert
            uri.Should().Be("https://base.com/resource?response_type=code&client_id=id&redirect_uri=uri&scope=scope&state=state");
        }

        [Test]
        public void Should_IssueCorrectRequestForAccessToken()
        {
            // act
            descendant.GetAccessToken("code", string.Empty);

            // assert
            client.BaseUrl.Should().Be("https://base.com");
            request.Resource.Should().Be("/resource");
            request.Method.Should().Be(Method.POST);

            request.Received(1).AddParameter(Arg.Is("client_id"), Arg.Is("id"));
            request.Received(1).AddParameter(Arg.Is("redirect_uri"), Arg.Is("uri"));
            request.Received(1).AddParameter(Arg.Is("client_secret"), Arg.Is("secret"));
            request.Received(1).AddParameter(Arg.Is("code"), Arg.Is("code"));
            request.Received(1).AddParameter(Arg.Is("grant_type"), Arg.Is("authorization_code"));

            client.Received(1).Execute(Arg.Is(request));
        }

        [Test]
        public void Should_ReturnObtainedAccessToken()
        {
            // act
            var token = descendant.GetAccessToken("code", string.Empty);

            // assert
            client.Received(1).Execute(Arg.Is(request));

            token.Should().Be("token");
        }

        [Test]
        public void Should_ThrowException_WhenAccessTokenIsRequestedAndErrorIsNotEmpty()
        {
            // act & assert
            descendant.Invoking(x => x.GetAccessToken("code", "error"))
                .ShouldThrow<ApplicationException>()
                .WithMessage("error");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void ShouldNot_ThrowException_WhenAccessTokenIsRequestedAndErrorIsEmpty(string error)
        {
            // act & assert
            descendant.Invoking(x => x.GetAccessToken("code", error)).ShouldNotThrow();
        }

        [Test]
        public void Should_IssueCorrectRequestForUserInfo()
        {
            // arrange
            response.Content.Returns("response");
            
            // act
            var info = descendant.GetUserInfo("token");

            // assert
            client.BaseUrl.Should().Be("https://base.com");
            request.Resource.Should().Be("/resource");
            request.Method.Should().Be(Method.GET);

            request.Received(1).AddParameter(Arg.Is("access_token"), Arg.Is("token"));

            client.Received(1).Execute(Arg.Is(request));

            info.Id.Should().Be("response");
            info.Email.Should().Be("Email1");
        }

        [Test]
        public void Should_OverwritePreviousAccessToken()
        {
            // arrange
            request.Parameters.Add(new Parameter
            {
                Name = "access_token",
                Value = "wrong"
            });

            // act
            descendant.GetUserInfo("token");

            // assert
            request.Parameters.Should().Contain(x => x.Name == "access_token" && (string) x.Value == "token");
        }

        class OAuth2ClientDescendant : OAuth2.Client.OAuth2Client
        {
            private readonly Endpoint endpoint = new Endpoint
            {
                BaseUri = "https://base.com",
                Resource = "/resource"
            };

            public OAuth2ClientDescendant(IRestClient client, IRestRequest request, IConfiguration configuration) : base(client, request, configuration)
            {
            }

            protected override Endpoint AccessCodeServiceEndpoint
            {
                get { return endpoint; }
            }

            protected override Endpoint AccessTokenServiceEndpoint
            {
                get { return endpoint; }
            }

            protected override Endpoint UserInfoServiceEndpoint
            {
                get { return endpoint; }
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