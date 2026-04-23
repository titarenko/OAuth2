using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Tests.Serialization
{
    [TestFixture]
    public class LoginCidadaoClientSerializationTests
    {
        private IRequestFactory _factory;
        private IClientConfiguration _configuration;
        private TestableLoginCidadaoClient _client;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableLoginCidadaoClient(_factory, _configuration);
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
