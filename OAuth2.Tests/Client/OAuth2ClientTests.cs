using System;
using System.Linq;
using System.Collections.Specialized;
using System.Net;
using FizzWare.NBuilder;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp.Portable;
using FluentAssertions;
using System.Threading.Tasks;
using System.Collections.Generic;
using RestSharp.Portable.Authenticators;
using System.Net.Http;

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

        private static System.Text.Encoding _encoding = System.Text.Encoding.UTF8;

        [SetUp]
        public void SetUp()
        {
            restRequest = Substitute.For<IRestRequest>();
            restResponse = Substitute.For<IRestResponse>();

            restResponse.StatusCode.Returns(HttpStatusCode.OK);
            restResponse.RawBytes.Returns(_encoding.GetBytes("response"));

            restClient = Substitute.For<IRestClient>();
            restClient.Execute(restRequest).Returns(Task.FromResult(restResponse));

            factory = Substitute.For<IRequestFactory>();
            factory.CreateClient().Returns(restClient);
            factory.CreateRequest(null).ReturnsForAnyArgs(restRequest);

            var configuration = Substitute.For<IClientConfiguration>();

            configuration.ClientId.Returns("client_id");
            configuration.ClientSecret.Returns("client_secret");
            configuration.RedirectUri.Returns("http://redirect-uri.net");
            configuration.Scope.Returns("scope");

            descendant = new OAuth2ClientDescendant(factory, configuration);
        }

        [Test]
        public void Should_ThrowUnexpectedResponse_When_CodeIsNotOk()
        {
            restResponse.StatusCode.Returns(HttpStatusCode.InternalServerError);

            descendant
                .Awaiting(x => x.GetUserInfo(new Dictionary<string, string>().ToLookup(y => y.Key, y => y.Value)))
                .ShouldThrow<UnexpectedResponseException>();
        }

        [Test]
        public void Should_ThrowUnexpectedResponse_When_ResponseIsEmpty()
        {
            restResponse.StatusCode.Returns(HttpStatusCode.OK);
            restResponse.RawBytes.Returns(_encoding.GetBytes(""));
            
            descendant
                .Awaiting(x => x.GetUserInfo(new Dictionary<string, string>().ToLookup(y => y.Key, y => y.Value)))
                .ShouldThrow<UnexpectedResponseException>();
        }

        [Test]
        public async Task Should_ReturnCorrectAccessCodeRequestUri()
        {
            // arrange
            //restClient.BuildUrl(restRequest).Returns(new Uri("https://login-link.net/"));

            // act
            var uri = await descendant.GetLoginLinkUri();

            // assert
            uri.Should().Be("https://accesscodeserviceendpoint/");

            factory.Received(1).CreateClient();
            factory.Received(1).CreateRequest("/AccessCodeServiceEndpoint");

            restClient.Received(1).BaseUrl = new Uri("https://AccessCodeServiceEndpoint");

            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "response_type" && (string)x.Value == "code"));
            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "client_id" && (string)x.Value == "client_id"));
            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "scope" && (string)x.Value == "scope"));
            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "redirect_uri" && (string)x.Value == "http://redirect-uri.net"));
            restRequest.Parameters.DidNotReceive().Add(Arg.Is<Parameter>(x => x.Name == "state"));
        }
        
        [Test]
        public void Should_ThrowException_WhenParametersForGetUserInfoContainError()
        {
            // arrange
            var parameters = new Dictionary<string, string> { { "error", "error2" } }.ToLookup(y => y.Key, y => y.Value);

            // act & assert
            descendant
                .Awaiting(x => x.GetUserInfo(parameters))
                .ShouldThrow<UnexpectedResponseException>()
                .And.FieldName.Should().Be("error");
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void ShouldNot_ThrowException_When_ParametersForGetUserInfoContainEmptyError(string error)
        {
            // arrange
            restResponse.RawBytes.Returns(_encoding.GetBytes("access_token=token"));

            // act & assert
            descendant
                .Awaiting(x => x.GetUserInfo(new Dictionary<string, string>
                {
                    {"error", error},
                    {"code", "code"}
                }.ToLookup(y => y.Key, y => y.Value)))
                .ShouldNotThrow();
        }

        [Test]
        public async Task Should_IssueCorrectRequestForAccessToken_When_GetUserInfoIsCalled()
        {
            // arrange
            restResponse.RawBytes.Returns(_encoding.GetBytes("access_token=token"));

            // act
            await descendant.GetUserInfo(new Dictionary<string, string> { { "code", "code" } }.ToLookup(y => y.Key, y => y.Value));

            // assert
            factory.Received(1).CreateRequest("/AccessTokenServiceEndpoint");
            restClient.Received(1).BaseUrl = new Uri("https://AccessTokenServiceEndpoint");
            restRequest.Received(1).Method = HttpMethod.Post;
            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "code" && (string)x.Value == "code"));
            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "client_id" && (string)x.Value == "client_id"));
            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "client_secret" && (string)x.Value == "client_secret"));
            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "redirect_uri" && (string)x.Value == "http://redirect-uri.net"));
            restRequest.Parameters.Received(1).Add(Arg.Is<Parameter>(x => x.Name == "grant_type" && (string)x.Value == "authorization_code"));
        }

        [Test]
        [TestCase("access_token=token")]
        [TestCase("{\"access_token\": \"token\"}")]
        public async Task Should_IssueCorrectRequestForUserInfo_When_GetUserInfoIsCalled(string response)
        {
            // arrange
            restResponse.RawBytes.Returns(_encoding.GetBytes(response));

            // act
            await descendant.GetUserInfo(new Dictionary<string, string> { { "code", "code" } }.ToLookup(y => y.Key, y => y.Value));

            // assert
            factory.Received(1).CreateRequest("/UserInfoServiceEndpoint");
            restClient.Received(1).BaseUrl = new Uri("https://UserInfoServiceEndpoint");
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