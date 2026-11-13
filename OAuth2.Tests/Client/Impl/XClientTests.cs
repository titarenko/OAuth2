using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
    public class XClientTests
    {
        /* lang=json */
        private const string Content = "{\n  \"name\": \"Matt Harris\",\n  \"profile_sidebar_border_color\": \"C0DEED\",\n  \"profile_background_tile\": false,\n  \"profile_sidebar_fill_color\": \"DDEEF6\",\n  \"location\": \"San Francisco\",\n  \"profile_image_url\": \"http://a1.twimg.com/profile_images/554181350/matt_normal.jpg\",\n  \"created_at\": \"Sat Feb 17 20:49:54 +0000 2007\",\n  \"profile_link_color\": \"0084B4\",\n  \"favourites_count\": 95,\n  \"url\": \"http://themattharris.com\",\n  \"contributors_enabled\": false,\n  \"utc_offset\": -28800,\n  \"id\": 777925,\n  \"profile_use_background_image\": true,\n  \"profile_text_color\": \"333333\",\n  \"protected\": false,\n  \"followers_count\": 1025,\n  \"lang\": \"en\",\n  \"verified\": false,\n  \"profile_background_color\": \"C0DEED\",\n  \"geo_enabled\": true,\n  \"notifications\": false,\n  \"description\": \"Developer Advocate at Twitter. Also a hacker and British expat who is married to @cindyli and lives in San Francisco.\",\n  \"time_zone\": \"Tijuana\",\n  \"friends_count\": 294,\n  \"statuses_count\": 2924,\n  \"profile_background_image_url\": \"http://s.twimg.com/a/1276711174/images/themes/theme1/bg.png\",\n  \"status\": {\n    \"coordinates\": {\n      \"coordinates\": [\n        -122.40075845,\n        37.78264991\n      ],\n      \"type\": \"Point\"\n    },\n    \"favorited\": false,\n    \"created_at\": \"Tue Jun 22 18:17:48 +0000 2010\",\n    \"truncated\": false,\n    \"text\": \"Going through and updating @twitterapi documentation\",\n    \"contributors\": null,\n    \"id\": 16789004997,\n    \"geo\": {\n      \"coordinates\": [\n        37.78264991,\n        -122.40075845\n      ],\n      \"type\": \"Point\"\n    },\n    \"in_reply_to_user_id\": null,\n    \"place\": null,\n    \"source\": \"<a href=\\\"http://itunes.apple.com/app/twitter/id333903271?mt=8\\\" rel=\\\"nofollow\\\">Twitter for iPhone</a>\",\n    \"in_reply_to_screen_name\": null,\n    \"in_reply_to_status_id\": null\n  },\n  \"screen_name\": \"themattharris\",\n  \"following\": false\n}";
        /* lang=json */
        private const string ContentWithNonStandardName = "{\n  \"name\": \"NonStandardName\",\n  \"profile_image_url\": \"http://a1.twimg.com/profile_images/554181350/matt_normal.jpg\",\n  \"id\": 777925\n}";

        private XClientDescendant _descendant = null!;

        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(callInfo =>
                new RestClient(new RestClientOptions(callInfo.Arg<string>())));
            factory.CreateRequest(Arg.Any<string>()).Returns(callInfo =>
                new RestRequest(callInfo.Arg<string>()));
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(callInfo =>
                new RestRequest(callInfo.Arg<string>(), callInfo.Arg<Method>()));

            _descendant = new XClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void RequestTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetRequestTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.twitter.com");
            endpoint.Resource.Should().Be("/oauth/request_token");
        }

        [Test]
        public void LoginEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetLoginServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.twitter.com");
            endpoint.Resource.Should().Be("/oauth/authenticate");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.twitter.com");
            endpoint.Resource.Should().Be("/oauth/access_token");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // arrange

            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.twitter.com");
            endpoint.Resource.Should().Be("/1.1/account/verify_credentials.json");
        }

        [Test]
        public async Task GetUserInfoAsync_OAuth1Flow_SignsActualRequestsWithMatchingSecrets()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse("oauth_token=request-token&oauth_token_secret=request-secret&oauth_callback_confirmed=true");
            handler.EnqueueResponse("oauth_token=access-token&oauth_token_secret=access-secret");
            // Selected fields from the existing verify_credentials fixture, with synthetic values.
            // https://docs.x.com/x-api/v1/accounts-and-users/manage-account-settings/api-reference/get-account-verify_credentials
            handler.EnqueueResponse(/* lang=json */ """{"id":12345,"name":"Example User","email":"user@example.com"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var restClient = new RestClient(httpClient, new RestClientOptions("https://api.twitter.com"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://api.twitter.com").Returns(restClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new XClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=",
                RedirectUri = "https://app.example.com/callback?next=a&value=b"
            });

            // Act
            var loginUri = await client.GetLoginLinkUriAsync();
            var info = await client.GetUserInfoAsync(new NameValueCollection
            {
                { "oauth_token", "request-token" }, { "oauth_verifier", "verifier+&=" }
            });

            // Assert
            loginUri.Should().Be("https://api.twitter.com/oauth/authenticate?oauth_token=request-token");
            handler.SentRequests.Should().HaveCount(3);
            handler.SentRequests[0].Method.Should().Be(HttpMethod.Post);
            handler.SentRequests[0].RequestUri!.AbsoluteUri.Should().Be("https://api.twitter.com/oauth/request_token");
            handler.SentRequests[1].Method.Should().Be(HttpMethod.Post);
            handler.SentRequests[1].RequestUri!.AbsoluteUri.Should().Be("https://api.twitter.com/oauth/access_token");
            handler.SentRequests[2].Method.Should().Be(HttpMethod.Get);
            handler.SentRequests[2].RequestUri!.AbsoluteUri.Should().Be("https://api.twitter.com/1.1/account/verify_credentials.json?include_email=true");
            var requestAuth = VerifySignature(handler.SentRequests[0], "");
            requestAuth["oauth_callback"].Should().Be("https://app.example.com/callback?next=a&value=b");
            requestAuth.Should().NotContainKey("oauth_token");
            var exchangeAuth = VerifySignature(handler.SentRequests[1], "request-secret");
            exchangeAuth["oauth_token"].Should().Be("request-token");
            exchangeAuth["oauth_verifier"].Should().Be("verifier+&=");
            var profileAuth = VerifySignature(handler.SentRequests[2], "access-secret");
            profileAuth["oauth_token"].Should().Be("access-token");
            foreach (var body in handler.SentBodies)
                body.Should().BeNullOrEmpty();
            info.Id.Should().Be("12345");
            info.Email.Should().Be("user@example.com");

            // Independently verify RFC 5849 section 3.4.1 against the emitted URI and OAuth header.
            static Dictionary<string, string> VerifySignature(HttpRequestMessage request, string tokenSecret)
            {
                request.Headers.Authorization!.Scheme.Should().Be("OAuth");
                var auth = request.Headers.Authorization.Parameter!.Split(',')
                    .Select(part => part.Trim().Split('=', 2))
                    .ToDictionary(parts => Uri.UnescapeDataString(parts[0]), parts => Uri.UnescapeDataString(parts[1].Trim('"')));
                auth["oauth_consumer_key"].Should().Be("client-id");
                auth["oauth_signature_method"].Should().Be("HMAC-SHA1");
                auth["oauth_nonce"].Should().NotBeNullOrEmpty();
                auth["oauth_timestamp"].Should().MatchRegex("^[0-9]+$");
                var query = HttpUtility.ParseQueryString(request.RequestUri!.Query);
                var parameters = auth.Where(pair => pair.Key != "oauth_signature")
                    .Concat(query.AllKeys.Select(key => new KeyValuePair<string, string>(key!, query[key]!)))
                    .Select(pair => new KeyValuePair<string, string>(Uri.EscapeDataString(pair.Key), Uri.EscapeDataString(pair.Value)))
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal).ThenBy(pair => pair.Value, StringComparer.Ordinal);
                var normalized = String.Join("&", parameters.Select(pair => pair.Key + "=" + pair.Value));
                var signatureBase = request.Method + "&" + Uri.EscapeDataString(request.RequestUri.GetLeftPart(UriPartial.Path)) + "&" + Uri.EscapeDataString(normalized);
                using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(Uri.EscapeDataString("secret+&=") + "&" + Uri.EscapeDataString(tokenSecret)));
                auth["oauth_signature"].Should().Be(Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase))));
                return auth;
            }
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange (uses Content const)

            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("777925");
            info.FirstName.Should().Be("Matt");
            info.LastName.Should().Be("Harris");
            info.Email.Should().BeNull();
            info.PhotoUri.Should().Be("http://a1.twimg.com/profile_images/554181350/matt_normal.jpg");
        }

        [Test]
        public void ParseUserInfo_NonStandardName_DoesNotThrow()
        {
            // arrange (uses ContentWithNonStandardName const)

            // act
            var info = _descendant.ParseUserInfo(ContentWithNonStandardName);

            // assert
            info.FirstName.Should().Be("NonStandardName");
            info.LastName.Should().BeNull();
        }

        class XClientDescendant : XClient
        {
            public XClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
