using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    [TestFixture]
    public class GitHubClientTests
    {
        // Legacy parser regression fixtures retained from the base branch.
        /* lang=json */
        private const string ContentWithNoName = "{  \"gists_url\": \"https://api.github.com/users/id/gists{/gist_id}\",  \"repos_url\": \"https://api.github.com/users/id/repos\",  \"following_url\": \"https://api.github.com/users/id/following{/other_user}\",  \"created_at\": \"2011-09-01T05:36:52+00:00\",  \"starred_url\": \"https://api.github.com/users/id/starred{/owner}{/repo}\",  \"login\": \"id\",  \"followers_url\": \"https://api.github.com/users/id/followers\",  \"type\": \"User\",  \"public_gists\": 0,  \"url\": \"https://api.github.com/users/id\",  \"subscriptions_url\": \"https://api.github.com/users/id/subscriptions\",  \"received_events_url\": \"https://api.github.com/users/id/received_events\",  \"followers\": 1,  \"avatar_url\": \"https://avatars.githubusercontent.com/u/123456?v=3\",  \"updated_at\": \"2015-04-1T05:27:31+00:00\",  \"events_url\": \"https://api.github.com/users/id/events{/privacy}\",  \"html_url\": \"https://github.com/id\",  \"following\": 23,  \"site_admin\": false,  \"id\": 123456,  \"public_repos\": 0,  \"gravatar_id\": \"\",  \"organizations_url\": \"https://api.github.com/users/id/orgs\"}";

        // Documented token-response fields with a dummy token, not a captured credential.
        // https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps
        /* lang=json */
        private const string TokenResponse = """{"access_token":"test-token","token_type":"bearer"}""";

        private GitHubClient _client = null!;
        private GitHubClientDescendant _descendant = null!;
        private IRequestFactory _factory = null!;
        private ClientConfiguration _configuration = null!;
        private MockHttpMessageHandler _handler = null!;
        private HttpClient _httpClient = null!;
        private RestClient _tokenClient = null!;
        private RestClient _apiClient = null!;

        [SetUp]
        public void SetUp()
        {
            _handler = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_handler);
            _tokenClient = new RestClient(_httpClient, new RestClientOptions("https://github.com"), disposeHttpClient: false);
            _apiClient = new RestClient(_httpClient, new RestClientOptions("https://api.github.com"), disposeHttpClient: false);
            _factory = Substitute.For<IRequestFactory>();
            _factory.CreateClient("https://github.com").Returns(_tokenClient);
            _factory.CreateClient("https://api.github.com").Returns(_apiClient);
            _factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call =>
                new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            _configuration = new ClientConfiguration
            {
                ClientId = "client-id",
                ClientSecret = "client-secret",
                RedirectUri = "https://app.example.com/callback"
            };
            _client = new GitHubClient(_factory, _configuration);
            _descendant = new GitHubClientDescendant(_factory, _configuration);
        }

        [TearDown]
        public void TearDown()
        {
            _apiClient.Dispose();
            _tokenClient.Dispose();
            _httpClient.Dispose();
        }

        [Test]
        public void AccessCodeEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // Arrange
            // The client is initialized in SetUp.

            // Act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // Assert
            endpoint.BaseUri.Should().Be("https://github.com");
            endpoint.Resource.Should().Be("/login/oauth/authorize");
        }

        [Test]
        public void AccessTokenEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // Arrange
            // The client is initialized in SetUp.

            // Act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // Assert
            endpoint.BaseUri.Should().Be("https://github.com");
            endpoint.Resource.Should().Be("/login/oauth/access_token");
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task GetCurrentTokenAsync_ExplicitRefreshToken_SendsRefreshRequest(bool jsonResponse)
        {
            // Arrange
            // GitHub's documented refresh contract, with synthetic tokens and credentials.
            // https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps#refreshing-an-access-token-with-a-refresh-token
            _configuration.ClientSecret = "secret+&=";
            _configuration.Scope = "repo offline_access";
            _handler.EnqueueResponse(jsonResponse
                ? /* lang=json */ """{"access_token":"refreshed-token","refresh_token":"rotated+&=","token_type":"bearer","expires_in":28800}"""
                : "access_token=refreshed-token&refresh_token=rotated%2B%26%3D&token_type=bearer&expires_in=28800");

            // Act
            var token = await _client.GetCurrentTokenAsync("refresh+&=", forceUpdate: true);

            // Assert
            var request = _handler.SentRequests.Should().ContainSingle().Which;
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsoluteUri.Should().Be("https://github.com/login/oauth/access_token");
            request.Headers.Authorization.Should().BeNull();
            request.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
            var form = HttpUtility.ParseQueryString(_handler.SentBodies[0]!);
            form.AllKeys.Should().BeEquivalentTo(new[] { "client_id", "client_secret", "grant_type", "refresh_token" });
            form["client_id"].Should().Be("client-id");
            form["client_secret"].Should().Be("secret+&=");
            form["grant_type"].Should().Be("refresh_token");
            form["refresh_token"].Should().Be("refresh+&=");
            token.Should().Be("refreshed-token");
            _client.AccessToken.Should().Be(token);
            _client.RefreshToken.Should().Be("rotated+&=");
            _client.TokenType.Should().Be("bearer");
        }

        [Test]
        public async Task GetCurrentTokenAsync_StoredRefreshToken_UsesRotatedToken()
        {
            // Arrange
            // Documented token fields; negative expiries deliberately exercise renewal without waiting.
            _handler.EnqueueResponse(/* lang=json */ """{"access_token":"initial-token","refresh_token":"initial+&=","expires_in":-1}""");
            await _client.GetTokenAsync(new NameValueCollection { { "code", "code" }, { "state", "callback-state" } });
            _handler.EnqueueResponse(/* lang=json */ """{"access_token":"first-token","refresh_token":"rotated+&=","expires_in":-1}""");
            _handler.EnqueueResponse(/* lang=json */ """{"access_token":"second-token","refresh_token":"final-token","expires_in":28800}""");

            // Act
            var firstToken = await _client.GetCurrentTokenAsync();
            var secondToken = await _client.GetCurrentTokenAsync();

            // Assert
            _handler.SentRequests.Should().HaveCount(3);
            var firstForm = HttpUtility.ParseQueryString(_handler.SentBodies[1]!);
            var secondForm = HttpUtility.ParseQueryString(_handler.SentBodies[2]!);
            firstForm["refresh_token"].Should().Be("initial+&=");
            secondForm["refresh_token"].Should().Be("rotated+&=");
            foreach (var form in new[] { firstForm, secondForm })
            {
                form.AllKeys.Should().BeEquivalentTo(new[] { "client_id", "client_secret", "grant_type", "refresh_token" });
                form["grant_type"].Should().Be("refresh_token");
            }
            firstToken.Should().Be("first-token");
            secondToken.Should().Be("second-token");
            _client.AccessToken.Should().Be(secondToken);
            _client.RefreshToken.Should().Be("final-token");
            _client.State.Should().Be("callback-state");
        }

        [TestCase(null)]
        [TestCase("ignored-refresh-token")]
        public async Task GetTokenAsync_AuthorizationCode_PreservesRequestParameters(string? unexpectedRefreshToken)
        {
            // Arrange
            _configuration.ClientSecret = "secret+&=";
            _configuration.RedirectUri = "https://app.example.com/callback?next=a&value=b";
            _handler.EnqueueResponse(TokenResponse);
            var parameters = new NameValueCollection { { "code", "code+&=" }, { "state", "state+&=" } };
            // An unexpected callback field must not switch an authorization-code exchange to refresh.
            if (unexpectedRefreshToken != null)
                parameters.Add("refresh_token", unexpectedRefreshToken);

            // Act
            var token = await _client.GetTokenAsync(parameters);

            // Assert
            var request = _handler.SentRequests.Should().ContainSingle().Which;
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsoluteUri.Should().Be("https://github.com/login/oauth/access_token");
            request.Headers.Authorization.Should().BeNull();
            request.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
            var form = HttpUtility.ParseQueryString(_handler.SentBodies[0]!);
            form.AllKeys.Should().BeEquivalentTo(new[] { "code", "client_id", "client_secret", "redirect_uri", "state" });
            form["code"].Should().Be("code+&=");
            form["client_id"].Should().Be("client-id");
            form["client_secret"].Should().Be("secret+&=");
            form["redirect_uri"].Should().Be(_configuration.RedirectUri);
            form["state"].Should().Be("state+&=");
            token.Should().Be("test-token");
            _client.State.Should().Be("state+&=");
        }

        [Test]
        public async Task GetTokenAsync_RefreshTokenWithoutCode_RejectsCallback()
        {
            // Arrange
            // Malformed callback: GetTokenAsync still requires an authorization code.
            var parameters = new NameValueCollection { { "refresh_token", "unexpected-token" } };

            // Act & Assert
            await _client.Awaiting(x => x.GetTokenAsync(parameters)).Should().ThrowAsync<UnexpectedResponseException>();
            _handler.SentRequests.Should().BeEmpty();
        }

        [Test]
        public async Task GetUserInfoAsync_DocumentedEmailResponse_PreservesVerification()
        {
            // Arrange
            EnqueuePrivateProfile(ProviderResponses.GitHubEmails);
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Id.Should().Be("1");
            info.ProviderName.Should().Be("GitHub");
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeTrue();
            info.ProviderData.Should().BeNull();
            _handler.SentRequests.Should().HaveCount(3);
            _handler.SentRequests[0].RequestUri!.AbsoluteUri.Should().Be("https://github.com/login/oauth/access_token");
            _handler.SentRequests[1].RequestUri!.AbsoluteUri.Should().Be("https://api.github.com/user");
            _handler.SentRequests[2].RequestUri!.AbsoluteUri.Should().Be("https://api.github.com/user/emails");
            _handler.SentRequests[2].Headers.Authorization!.Scheme.Should().Be("Bearer");
            _handler.SentRequests[2].Headers.Authorization!.Parameter.Should().Be("test-token");
        }

        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.TooManyRequests)]
        [TestCase(HttpStatusCode.InternalServerError)]
        public async Task GetUserInfoAsync_EmailEndpointFailure_ThrowsUnexpectedResponse(HttpStatusCode statusCode)
        {
            // Arrange
            _handler.EnqueueResponse(TokenResponse);
            var profile = JsonNode.Parse(ProviderResponses.GitHubUser)!.AsObject();
            profile["email"] = null;
            _handler.EnqueueResponse(profile.ToJsonString());
            // Deliberately attach plausible claims to an error response; status must be checked first.
            _handler.EnqueueResponse(statusCode, ProviderResponses.GitHubEmails);
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act & Assert
            var exception = await _client.Awaiting(x => x.GetUserInfoAsync(parameters)).Should().ThrowAsync<UnexpectedResponseException>();
            exception.Which.Response!.StatusCode.Should().Be(statusCode);
            _handler.SentRequests.Should().HaveCount(3);
        }

        [TestCase(/* lang=json */ "[]")]
        [TestCase(/* lang=json */ "null")]
        public async Task GetUserInfoAsync_EmptyEmailCollection_ReturnsNoEmail(string content)
        {
            // Arrange
            // Defensive parser cases, not additional published successful responses.
            EnqueuePrivateProfile(content);
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().BeNull();
            info.EmailVerified.Should().BeNull();
            _handler.SentRequests.Should().HaveCount(3);
        }

        [TestCase(/* lang=json */ "null")]
        [TestCase(/* lang=json */ "\"true\"")]
        [TestCase(/* lang=json */ "1")]
        [TestCase(/* lang=json */ "{}")]
        [TestCase(/* lang=json */ "[]")]
        public async Task GetUserInfoAsync_MalformedVerification_ThrowsJsonException(string verification)
        {
            // Arrange
            // Invalid claim types must retain the existing typed parser's rejection behavior.
            var emails = JsonNode.Parse(ProviderResponses.GitHubEmails)!.AsArray();
            emails[0]!["verified"] = JsonNode.Parse(verification);
            EnqueuePrivateProfile(emails.ToJsonString());
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act & Assert
            await _client.Awaiting(x => x.GetUserInfoAsync(parameters)).Should().ThrowAsync<JsonException>();
            _handler.SentRequests.Should().HaveCount(3);
        }

        [Test]
        public async Task GetUserInfoAsync_MissingPrimaryVerification_DoesNotUseSecondaryVerification()
        {
            // Arrange
            // Contract variation: the selected primary record omits verification.
            var emails = JsonNode.Parse(ProviderResponses.GitHubEmails)!.AsArray();
            var secondary = emails[0]!.DeepClone();
            secondary["email"] = "secondary@example.com";
            secondary["primary"] = false;
            emails[0]!.AsObject().Remove("verified");
            emails.Add(secondary);
            EnqueuePrivateProfile(emails.ToJsonString());
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeNull();
        }

        [Test]
        public async Task GetUserInfoAsync_NoPrimaryEmail_SelectsVerifiedEmail()
        {
            // Arrange
            // Selection variation using the documented email-record schema.
            var emails = JsonNode.Parse(ProviderResponses.GitHubEmails)!.AsArray();
            emails[0]!["primary"] = false;
            var secondary = emails[0]!.DeepClone();
            secondary["email"] = "secondary@example.com";
            emails[0]!["verified"] = false;
            emails.Add(secondary);
            EnqueuePrivateProfile(emails.ToJsonString());
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().Be("secondary@example.com");
            info.EmailVerified.Should().BeTrue();
        }

        [Test]
        public async Task GetUserInfoAsync_NoPrimaryOrVerifiedEmail_SelectsFirstEmail()
        {
            // Arrange
            var emails = JsonNode.Parse(ProviderResponses.GitHubEmails)!.AsArray();
            emails[0]!["primary"] = false;
            emails[0]!["verified"] = false;
            var secondary = emails[0]!.DeepClone();
            secondary["email"] = "secondary@example.com";
            emails.Add(secondary);
            EnqueuePrivateProfile(emails.ToJsonString());
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeFalse();
        }

        [Test]
        public async Task GetUserInfoAsync_OverriddenEmailParser_HonorsSelectedVerification()
        {
            // Arrange
            var client = new CustomEmailGitHubClient(_factory, _configuration);
            // The override changes verification while preserving the response's email address.
            EnqueuePrivateProfile(ProviderResponses.GitHubEmails);
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeFalse();
            _handler.SentRequests.Should().HaveCount(3);
        }

        [Test]
        public async Task GetUserInfoAsync_PublicEmail_DoesNotRequestPrivateEmails()
        {
            // Arrange
            _handler.EnqueueResponse(TokenResponse);
            _handler.EnqueueResponse(ProviderResponses.GitHubUser);
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeNull();
            info.ProviderData.Should().BeNull();
            _handler.SentRequests.Should().HaveCount(2);
            _handler.SentRequests[1].RequestUri!.AbsoluteUri.Should().Be("https://api.github.com/user");
        }

        [Test]
        public async Task GetUserInfoAsync_RepeatedResponses_DoesNotReuseVerification()
        {
            // Arrange
            EnqueuePrivateProfile(ProviderResponses.GitHubEmails);
            await _client.GetUserInfoAsync(new NameValueCollection { { "code", "first-code" } });
            var emails = JsonNode.Parse(ProviderResponses.GitHubEmails)!.AsArray();
            emails[0]!.AsObject().Remove("verified");
            EnqueuePrivateProfile(emails.ToJsonString());
            var parameters = new NameValueCollection { { "code", "second-code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeNull();
            _handler.SentRequests.Should().HaveCount(6);
        }

        [TestCase(null)]
        [TestCase("")]
        public async Task GetUserInfoAsync_UnusablePrimaryEmail_SelectsUsableEmail(string? email)
        {
            // Arrange
            // Defensive missing/empty address variation; never attach verification without an address.
            var emails = JsonNode.Parse(ProviderResponses.GitHubEmails)!.AsArray();
            var secondary = emails[0]!.DeepClone();
            secondary["primary"] = false;
            secondary["verified"] = false;
            if (email is null)
                emails[0]!.AsObject().Remove("email");
            else
                emails[0]!["email"] = email;
            emails.Add(secondary);
            EnqueuePrivateProfile(emails.ToJsonString());
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeFalse();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task GetUserInfoAsync_UnverifiedPrimaryEmail_PreservesPrimaryVerification(bool secondaryFirst)
        {
            // Arrange
            var emails = JsonNode.Parse(ProviderResponses.GitHubEmails)!.AsArray();
            var secondary = emails[0]!.DeepClone();
            secondary["email"] = "secondary@example.com";
            secondary["primary"] = false;
            emails[0]!["verified"] = false;
            emails.Insert(secondaryFirst ? 0 : 1, secondary);
            EnqueuePrivateProfile(emails.ToJsonString());
            var parameters = new NameValueCollection { { "code", "code" } };

            // Act
            var info = await _client.GetUserInfoAsync(parameters);

            // Assert
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeFalse();
        }

        [Test]
        public void ParseUserInfo_MissingName_FallsBackToLogin()
        {
            // Arrange
            var response = JsonNode.Parse(ProviderResponses.GitHubUser)!.AsObject();
            response.Remove("name");
            response.Remove("email");
            var content = response.ToJsonString();

            // Act
            var info = _descendant.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("1");
            info.FirstName.Should().Be("octocat");
            info.LastName.Should().BeEmpty();
            info.Email.Should().BeNull();
            info.PhotoUri.Should().Be("https://github.com/images/error/octocat_happy.gif");
        }

        [Test]
        public void ParseUserInfo_SourcedResponse_ReturnsCorrectFields()
        {
            // Arrange
            var content = ProviderResponses.GitHubUser;

            // Act
            var info = _descendant.ParseUserInfo(content);

            // Assert
            info.Id.Should().Be("1");
            info.FirstName.Should().Be("monalisa");
            info.LastName.Should().Be("octocat");
            info.Email.Should().Be("octocat@example.com");
            info.EmailVerified.Should().BeNull();
            info.PhotoUri.Should().Be("https://github.com/images/error/octocat_happy.gif");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // Arrange
            // Legacy regression payload from this repository; placeholder login and numeric ID.

            // Act
            var info = _descendant.ParseUserInfo(ContentWithNoName);

            // Assert
            info.Id.Should().Be("123456");
            info.FirstName.Should().Be("id");
            info.LastName.Should().Be(String.Empty);
            info.Email.Should().Be(null);
            info.PhotoUri.Should().Be("https://avatars.githubusercontent.com/u/123456?v=3");
        }

        [Test]
        public void UserInfoEndpoint_Default_ReturnsCorrectEndpoint()
        {
            // Arrange
            // The client is initialized in SetUp.

            // Act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // Assert
            endpoint.BaseUri.Should().Be("https://api.github.com");
            endpoint.Resource.Should().Be("/user");
        }

        private void EnqueuePrivateProfile(string emails)
        {
            _handler.EnqueueResponse(TokenResponse);
            var profile = JsonNode.Parse(ProviderResponses.GitHubUser)!.AsObject();
            profile["email"] = null;
            _handler.EnqueueResponse(profile.ToJsonString());
            _handler.EnqueueResponse(emails);
        }

        private class GitHubClientDescendant : GitHubClient
        {
            public GitHubClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public Endpoint GetAccessCodeServiceEndpoint() => AccessCodeServiceEndpoint;
            public Endpoint GetAccessTokenServiceEndpoint() => AccessTokenServiceEndpoint;
            public Endpoint GetUserInfoServiceEndpoint() => UserInfoServiceEndpoint;
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class CustomEmailGitHubClient : GitHubClient
        {
            public CustomEmailGitHubClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            protected override List<UserEmails> ParseEmailAddresses(string content)
            {
                var emails = base.ParseEmailAddresses(content);
                foreach (var email in emails)
                    email.Verified = false;

                return emails;
            }
        }
    }
}
