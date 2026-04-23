using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using OAuth2.Tests.TestHelpers;
using RestSharp;

namespace OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class FacebookClientTests
    {
        /* lang=json */
        private const string Content = "{\"email\":\"email\",\"first_name\":\"name\",\"last_name\":\"surname\",\"id\":\"id\",\"picture\":{\"data\":{\"url\":\"picture\"}}}";

        private FacebookClientDescendant _descendant;
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

            _descendant = new FacebookClientDescendant(_factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://www.facebook.com");
            endpoint.Resource.Should().Be("/v25.0/dialog/oauth");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://graph.facebook.com");
            endpoint.Resource.Should().Be("/v25.0/oauth/access_token");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://graph.facebook.com");
            endpoint.Resource.Should().Be("/v25.0/me");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("id");
            info.FirstName.Should().Be("name");
            info.LastName.Should().Be("surname");
            info.Email.Should().Be("email");
            info.PhotoUri.Should().StartWith("picture");
        }

        [Test]
        public async Task GetUserInfo_Called_AddsFieldsParameter()
        {
            // arrange
            _handler.EnqueueResponse("access_token=token");
            _handler.EnqueueResponse(Content);

            // act
            await _descendant.GetUserInfoAsync(new NameValueCollection
            {
                { "code", "code" }
            });

            // assert
            var userInfoRequest = _capturedRequests.Last();
            userInfoRequest.Parameters.FirstOrDefault(p => String.Equals(p.Name, "fields", StringComparison.Ordinal))?.Value
                .Should().Be("id,first_name,last_name,email,picture");
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