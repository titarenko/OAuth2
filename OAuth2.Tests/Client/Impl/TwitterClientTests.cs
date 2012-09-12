using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Tests.Client.Impl
{
    public class TwitterClientTests
    {
        private const string Content = "{\n  \"name\": \"Matt Harris\",\n  \"profile_sidebar_border_color\": \"C0DEED\",\n  \"profile_background_tile\": false,\n  \"profile_sidebar_fill_color\": \"DDEEF6\",\n  \"location\": \"San Francisco\",\n  \"profile_image_url\": \"http://a1.twimg.com/profile_images/554181350/matt_normal.jpg\",\n  \"created_at\": \"Sat Feb 17 20:49:54 +0000 2007\",\n  \"profile_link_color\": \"0084B4\",\n  \"favourites_count\": 95,\n  \"url\": \"http://themattharris.com\",\n  \"contributors_enabled\": false,\n  \"utc_offset\": -28800,\n  \"id\": 777925,\n  \"profile_use_background_image\": true,\n  \"profile_text_color\": \"333333\",\n  \"protected\": false,\n  \"followers_count\": 1025,\n  \"lang\": \"en\",\n  \"verified\": false,\n  \"profile_background_color\": \"C0DEED\",\n  \"geo_enabled\": true,\n  \"notifications\": false,\n  \"description\": \"Developer Advocate at Twitter. Also a hacker and British expat who is married to @cindyli and lives in San Francisco.\",\n  \"time_zone\": \"Tijuana\",\n  \"friends_count\": 294,\n  \"statuses_count\": 2924,\n  \"profile_background_image_url\": \"http://s.twimg.com/a/1276711174/images/themes/theme1/bg.png\",\n  \"status\": {\n    \"coordinates\": {\n      \"coordinates\": [\n        -122.40075845,\n        37.78264991\n      ],\n      \"type\": \"Point\"\n    },\n    \"favorited\": false,\n    \"created_at\": \"Tue Jun 22 18:17:48 +0000 2010\",\n    \"truncated\": false,\n    \"text\": \"Going through and updating @twitterapi documentation\",\n    \"contributors\": null,\n    \"id\": 16789004997,\n    \"geo\": {\n      \"coordinates\": [\n        37.78264991,\n        -122.40075845\n      ],\n      \"type\": \"Point\"\n    },\n    \"in_reply_to_user_id\": null,\n    \"place\": null,\n    \"source\": \"<a href=\\\"http://itunes.apple.com/app/twitter/id333903271?mt=8\\\" rel=\\\"nofollow\\\">Twitter for iPhone</a>\",\n    \"in_reply_to_screen_name\": null,\n    \"in_reply_to_status_id\": null\n  },\n  \"screen_name\": \"themattharris\",\n  \"following\": false\n}";
        private const string ContentWithNonStandardName = "{\n  \"name\": \"NonStandardName\",\n  \"profile_image_url\": \"http://a1.twimg.com/profile_images/554181350/matt_normal.jpg\",\n  \"id\": 777925\n}";

        private TwitterClientDescendant descendant;

        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<IRequestFactory>();
            factory.NewClient().Returns(Substitute.For<IRestClient>());
            factory.NewRequest().Returns(Substitute.For<IRestRequest>());

            var configurationManager = Substitute.For<IConfigurationManager>();
            configurationManager
                .GetConfigSection<OAuth2ConfigurationSection>("oauth2")
                .Returns(new OAuth2ConfigurationSection());

            descendant = new TwitterClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectRequestTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetRequestTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.twitter.com");
            endpoint.Resource.Should().Be("/oauth/request_token");
        }

        [Test]
        public void Should_ReturnCorrectLoginServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetLoginServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.twitter.com");
            endpoint.Resource.Should().Be("/oauth/authenticate");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.twitter.com");
            endpoint.Resource.Should().Be("/oauth/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.twitter.com");
            endpoint.Resource.Should().Be("/account/verify_credentials.json");
        }
        
        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("777925");
            info.FirstName.Should().Be("Matt");
            info.LastName.Should().Be("Harris");
            info.Email.Should().BeNull();
            info.PhotoUri.Should().Be("http://a1.twimg.com/profile_images/554181350/matt_normal.jpg");
        }

        [Test]
        public void ShouldNot_ThrowException_WhenNonStandardNameIsPresentInContent()
        {
            // act
            var info = descendant.ParseUserInfo(ContentWithNonStandardName);

            // assert
            info.FirstName.Should().Be("NonStandardName");
            info.LastName.Should().BeNull();
        }

        class TwitterClientDescendant : TwitterClient
        {
            public TwitterClientDescendant(IRequestFactory factory, IClientConfiguration configuration) 
                : base(factory, configuration)
            {
            }

            public Endpoint GetRequestTokenServiceEndpoint()
            {
                return RequestTokenServiceEndpoint;
            }

            public Endpoint GetLoginServiceEndpoint()
            {
                return LoginServiceEndpoint;
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