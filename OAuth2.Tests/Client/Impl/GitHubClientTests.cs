using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Tests.Client.Impl
{
    [TestFixture]
    public class GitHubClientTests
    {
        private const string ContentWithNoName = "{  \"gists_url\": \"https://api.github.com/users/id/gists{/gist_id}\",  \"repos_url\": \"https://api.github.com/users/id/repos\",  \"following_url\": \"https://api.github.com/users/id/following{/other_user}\",  \"created_at\": \"2011-09-01T05:36:52+00:00\",  \"starred_url\": \"https://api.github.com/users/id/starred{/owner}{/repo}\",  \"login\": \"id\",  \"followers_url\": \"https://api.github.com/users/id/followers\",  \"type\": \"User\",  \"public_gists\": 0,  \"url\": \"https://api.github.com/users/id\",  \"subscriptions_url\": \"https://api.github.com/users/id/subscriptions\",  \"received_events_url\": \"https://api.github.com/users/id/received_events\",  \"followers\": 1,  \"avatar_url\": \"https://avatars.githubusercontent.com/u/123456?v=3\",  \"updated_at\": \"2015-04-1T05:27:31+00:00\",  \"events_url\": \"https://api.github.com/users/id/events{/privacy}\",  \"html_url\": \"https://github.com/id\",  \"following\": 23,  \"site_admin\": false,  \"id\": 123456,  \"public_repos\": 0,  \"gravatar_id\": \"\",  \"organizations_url\": \"https://api.github.com/users/id/orgs\"}";

        private GitHubClientDescendant _descendant;

        [SetUp]
        public void SetUp()
        {
            _descendant = new GitHubClientDescendant(Substitute.For<IRequestFactory>(), Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://github.com");
            endpoint.Resource.Should().Be("/login/oauth/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://github.com");
            endpoint.Resource.Should().Be("/login/oauth/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.github.com");
            endpoint.Resource.Should().Be("/user");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = _descendant.ParseUserInfo(ContentWithNoName);

            //  assert
            info.Id.Should().Be("123456");
            info.FirstName.Should().Be("id");
            info.LastName.Should().Be(string.Empty);
            info.Email.Should().Be(null);
            info.PhotoUri.Should().Be("https://avatars.githubusercontent.com/u/123456?v=3");
        }

        class GitHubClientDescendant : GitHubClient
        {
            public GitHubClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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