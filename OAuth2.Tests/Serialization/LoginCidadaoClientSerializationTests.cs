using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2.Tests.TestHelpers;
using RestSharp;

namespace OAuth2.Tests.Serialization
{
    [TestFixture]
    public class LoginCidadaoClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableLoginCidadaoClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableLoginCidadaoClient(_factory, _configuration);
        }

        [Test]
        public async Task GetUserInfoAsync_AuthorizationCode_SendsExpectedRequests()
        {
            // Arrange
            using var handler = new MockHttpMessageHandler();
            handler.EnqueueResponse(/* lang=json */ """{"access_token":"test-token"}""");
            // Existing LoginCidadao parser fixture shape, with synthetic profile values; tests transport, not live API availability.
            handler.EnqueueResponse(/* lang=json */ """{"first_name":"Carlos","last_name":"Silva","cpf":"000.000.000-00","email":"user@example.com"}""");
            using var httpClient = new HttpClient(handler, disposeHandler: false);
            using var tokenClient = new RestClient(httpClient, new RestClientOptions("https://logincidadao.rs.gov.br"), disposeHttpClient: false);
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient("https://logincidadao.rs.gov.br").Returns(tokenClient);
            factory.CreateRequest(Arg.Any<string>(), Arg.Any<Method>()).Returns(call => new RestRequest(call.Arg<string>(), call.Arg<Method>()));
            var client = new LoginCidadaoClient(factory, new ClientConfiguration
            {
                ClientId = "client-id", ClientSecret = "secret+&=", ClientPublic = "public-key",
                RedirectUri = "https://app.example.com/callback?next=a&value=b"
            });

            // Act
            var info = await client.GetUserInfoAsync(new NameValueCollection { { "code", "code+&=" } });

            // Assert
            handler.SentRequests.Should().HaveCount(2);
            var tokenRequest = handler.SentRequests[0];
            tokenRequest.Method.Should().Be(HttpMethod.Post);
            tokenRequest.RequestUri!.AbsoluteUri.Should().Be("https://logincidadao.rs.gov.br/openid/connect/token");
            tokenRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
            var form = HttpUtility.ParseQueryString(handler.SentBodies[0]!);
            form["code"].Should().Be("code+&=");
            form["client_id"].Should().Be("client-id");
            form["client_secret"].Should().Be("secret+&=");
            form["grant_type"].Should().Be("authorization_code");
            form["redirect_uri"].Should().Be("https://app.example.com/callback?next=a&value=b");
            tokenRequest.Headers.Authorization.Should().BeNull();
            var request = handler.SentRequests[1];
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.GetLeftPart(System.UriPartial.Path).Should().Be("https://logincidadao.rs.gov.br/api/v2/person");
            handler.SentBodies[1].Should().BeNullOrEmpty();
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            request.Headers.Authorization!.ToString().Should().Be("Bearer test-token");
            query["access_token"].Should().BeNull();
            query["oauth_token"].Should().BeNull();
            query["client_secret"].Should().BeNull();
            info.ProviderName.Should().Be(client.Name);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Carlos"",""last_name"":""Silva"",""cpf"":""123.456.789-00"",""email"":""carlos@gov.br""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.FirstName.Should().Be("Carlos");
            info.LastName.Should().Be("Silva");
            info.Email.Should().Be("carlos@gov.br");
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCidadaoType()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Carlos"",""last_name"":""Silva"",""cpf"":""123.456.789-00"",""email"":""carlos@gov.br""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Should().BeOfType<LoginCidadaoClient.Cidadao>();
        }

        [Test]
        public void ParseUserInfo_ValidContent_SetsCpf()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Carlos"",""last_name"":""Silva"",""cpf"":""123.456.789-00"",""email"":""c@g.br""}";

            // act
            var info = (LoginCidadaoClient.Cidadao)_client.ParseUserInfo(content);

            // assert
            info.Cpf.Should().Be("123.456.789-00");
        }

        [Test]
        public void ParseUserInfo_CidadaoResult_SerializesCpfField()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""Carlos"",""last_name"":""Silva"",""cpf"":""123.456.789-00"",""email"":""carlos@gov.br""}";

            // act
            var info = (LoginCidadaoClient.Cidadao)_client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Cpf").GetString().Should().Be("123.456.789-00");
        }

        [Test]
        public void ParseUserInfo_MissingEmail_ReturnsNullEmail()
        {
            // arrange
            /* lang=json */
            const string content = @"{""first_name"":""A"",""last_name"":""B"",""cpf"":""000.000.000-00""}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Email.Should().BeNull();
        }

        private class TestableLoginCidadaoClient : LoginCidadaoClient
        {
            public TestableLoginCidadaoClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
