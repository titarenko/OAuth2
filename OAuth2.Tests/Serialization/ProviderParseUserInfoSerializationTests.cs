using System;
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;

namespace OAuth2.Tests.Serialization
{
    /// <summary>
    /// Tests that each provider's ParseUserInfo output serializes to correct JSON and
    /// that the JSON shape is stable across serialization library changes.
    /// </summary>
    [TestFixture]
    public class ProviderParseUserInfoSerializationTests
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        private IRequestFactory _factory;
        private IClientConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
        }

        private static void AssertUserInfoSerializesCorrectly(UserInfo info)
        {
            var json = JsonSerializer.Serialize(info, Options);
            json.Should().NotBeNullOrEmpty();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Verify structural integrity
            root.ValueKind.Should().Be(JsonValueKind.Object);
            root.TryGetProperty("AvatarUri", out var avatar).Should().BeTrue();
            avatar.ValueKind.Should().Be(JsonValueKind.Object);
        }

        private static void AssertFieldRoundtrips(UserInfo info, string propertyName, string expectedValue)
        {
            var json = JsonSerializer.Serialize(info, Options);
            using var doc = JsonDocument.Parse(json);
            if (expectedValue != null)
                doc.RootElement.GetProperty(propertyName).GetString().Should().Be(expectedValue);
        }

        #region Asana

        /* lang=json */
        private const string AsanaContent = @"{
            ""data"": {
                ""id"": ""12345"",
                ""name"": ""John Doe"",
                ""email"": ""john@asana.com"",
                ""photo"": {
                    ""image_36x36"": ""https://asana.com/photo_36.jpg"",
                    ""image_60x60"": ""https://asana.com/photo_60.jpg"",
                    ""image_128x128"": ""https://asana.com/photo_128.jpg""
                }
            }
        }";

        [Test]
        public void Asana_Should_ParseAndSerializeUserInfo()
        {
            var client = new AsanaClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(AsanaContent);

            info.Id.Should().Be("12345");
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Doe");
            info.Email.Should().Be("john@asana.com");
            info.AvatarUri.Small.Should().Be("https://asana.com/photo_36.jpg");
            info.AvatarUri.Normal.Should().Be("https://asana.com/photo_60.jpg");
            info.AvatarUri.Large.Should().Be("https://asana.com/photo_128.jpg");

            AssertUserInfoSerializesCorrectly(info);
            AssertFieldRoundtrips(info, "Id", "12345");
            AssertFieldRoundtrips(info, "FirstName", "John");
            AssertFieldRoundtrips(info, "LastName", "Doe");
            AssertFieldRoundtrips(info, "Email", "john@asana.com");
        }

        [Test]
        public void Asana_Should_HandleSingleName()
        {
            /* lang=json */
            const string content = @"{""data"":{""id"":""1"",""name"":""Madonna"",""email"":null,""photo"":{""image_36x36"":""s.jpg"",""image_60x60"":""n.jpg"",""image_128x128"":""l.jpg""}}}";
            var client = new AsanaClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.FirstName.Should().Be("Madonna");
            info.LastName.Should().BeEmpty();
            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Asana_Should_ReturnEmptyUserInfo_WhenNoData()
        {
            /* lang=json */
            const string content = @"{}";
            var client = new AsanaClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.Id.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region DigitalOcean

        /* lang=json */
        private const string DigitalOceanContent = @"{""uid"":""do-123"",""info"":{""name"":""Jane Smith"",""email"":""jane@do.com""}}";

        [Test]
        public void DigitalOcean_Should_ParseAndSerializeUserInfo()
        {
            var client = new DigitalOceanClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(DigitalOceanContent);

            info.Id.Should().Be("do-123");
            info.FirstName.Should().Be("Jane Smith");
            info.LastName.Should().BeEmpty();
            info.Email.Should().Be("jane@do.com");

            AssertUserInfoSerializesCorrectly(info);
            AssertFieldRoundtrips(info, "Id", "do-123");
        }

        [Test]
        public void DigitalOcean_Should_HandleNullEmail()
        {
            /* lang=json */
            const string content = @"{""uid"":""1"",""info"":{""name"":""Test""}}";
            var client = new DigitalOceanClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.Email.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region ExactOnline

        /* lang=json */
        private const string ExactOnlineContent = @"{""id"":""exact-1"",""display_name"":""Alice"",""email"":""alice@exact.com"",""images"":[{""url"":""https://exact.com/avatar.jpg""}]}";

        [Test]
        public void ExactOnline_Should_ParseAndSerializeUserInfo()
        {
            var client = new ExactOnlineClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(ExactOnlineContent);

            info.Id.Should().Be("exact-1");
            info.FirstName.Should().Be("Alice");
            info.Email.Should().Be("alice@exact.com");
            info.AvatarUri.Small.Should().Be("https://exact.com/avatar.jpg");
            info.AvatarUri.Normal.Should().Be("https://exact.com/avatar.jpg");
            info.AvatarUri.Large.Should().Be("https://exact.com/avatar.jpg");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void ExactOnline_Should_HandleMissingImages()
        {
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""Bob"",""email"":""bob@test.com""}";
            var client = new ExactOnlineClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.AvatarUri.Normal.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Facebook

        /* lang=json */
        private const string FacebookContent = @"{""id"":""fb-123"",""first_name"":""Mark"",""last_name"":""User"",""email"":""mark@fb.com"",""picture"":{""data"":{""url"":""https://fb.com/pic.jpg""}}}";

        [Test]
        public void Facebook_Should_ParseAndSerializeUserInfo()
        {
            var client = new FacebookClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(FacebookContent);

            info.Id.Should().Be("fb-123");
            info.FirstName.Should().Be("Mark");
            info.LastName.Should().Be("User");
            info.Email.Should().Be("mark@fb.com");
            info.AvatarUri.Small.Should().StartWith("https://fb.com/pic.jpg");
            info.AvatarUri.Normal.Should().StartWith("https://fb.com/pic.jpg");
            info.AvatarUri.Large.Should().StartWith("https://fb.com/pic.jpg");
            info.AvatarUri.Small.Should().Contain("small");
            info.AvatarUri.Normal.Should().Contain("normal");
            info.AvatarUri.Large.Should().Contain("large");

            AssertUserInfoSerializesCorrectly(info);
            AssertFieldRoundtrips(info, "Id", "fb-123");
            AssertFieldRoundtrips(info, "FirstName", "Mark");
        }

        [Test]
        public void Facebook_Should_HandleNullEmail()
        {
            /* lang=json */
            const string content = @"{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""picture"":{""data"":{""url"":""pic.jpg""}}}";
            var client = new FacebookClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.Email.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Fitbit

        /* lang=json */
        private const string FitbitContent = @"{""user"":{""encodedId"":""fitbit-1"",""fullName"":""Jane Doe"",""displayName"":""JaneDoe"",""avatar"":""https://fitbit.com/avatar.jpg""}}";

        [Test]
        public void Fitbit_Should_ParseAndSerializeUserInfo()
        {
            var client = new FitbitClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(FitbitContent);

            info.Id.Should().Be("fitbit-1");
            info.FirstName.Should().Be("Jane");
            info.LastName.Should().Be("Doe");
            info.AvatarUri.Normal.Should().Be("https://fitbit.com/avatar.jpg");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Fitbit_Should_FallbackToDisplayName_WhenFullNameIsSingleWord()
        {
            /* lang=json */
            const string content = @"{""user"":{""encodedId"":""1"",""fullName"":""Cher"",""displayName"":""CherFitness"",""avatar"":""a.jpg""}}";
            var client = new FitbitClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.FirstName.Should().Be("Cher");
            info.LastName.Should().BeEmpty();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Foursquare

        /* lang=json */
        private const string FoursquareContent = @"{""response"":{""user"":{""id"":""4sq-1"",""firstName"":""Alice"",""lastName"":""Wonder"",""contact"":{""email"":""alice@4sq.com""},""photo"":{""prefix"":""https://4sq.com/img/"",""suffix"":""/photo.jpg""}}}}";

        [Test]
        public void Foursquare_Should_ParseAndSerializeUserInfo()
        {
            var client = new FoursquareClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(FoursquareContent);

            info.Id.Should().Be("4sq-1");
            info.FirstName.Should().Be("Alice");
            info.LastName.Should().Be("Wonder");
            info.Email.Should().Be("alice@4sq.com");
            info.AvatarUri.Small.Should().Contain("36x36");
            info.AvatarUri.Normal.Should().NotBeEmpty();
            info.AvatarUri.Large.Should().Contain("300x300");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Foursquare_Should_HandleMissingEmail()
        {
            /* lang=json */
            const string content = @"{""response"":{""user"":{""id"":""1"",""firstName"":""A"",""lastName"":""B"",""contact"":{},""photo"":{""prefix"":""p/"",""suffix"":"".jpg""}}}}";
            var client = new FoursquareClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.Email.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region GitHub

        /* lang=json */
        private const string GitHubContent = @"{""id"":123,""login"":""octocat"",""name"":""Octo Cat"",""email"":""octo@github.com"",""avatar_url"":""https://github.com/avatar.jpg""}";

        [Test]
        public void GitHub_Should_ParseAndSerializeUserInfo()
        {
            var client = new GitHubClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(GitHubContent);

            info.Id.Should().Be("123");
            info.FirstName.Should().Be("Octo");
            info.LastName.Should().Be("Cat");
            info.Email.Should().Be("octo@github.com");
            info.ProviderName.Should().Be("GitHub");
            info.AvatarUri.Small.Should().Contain("s=36");
            info.AvatarUri.Normal.Should().Be("https://github.com/avatar.jpg");
            info.AvatarUri.Large.Should().Contain("s=300");

            AssertUserInfoSerializesCorrectly(info);
            AssertFieldRoundtrips(info, "ProviderName", "GitHub");
        }

        [Test]
        public void GitHub_Should_FallbackToLogin_WhenNameIsNull()
        {
            /* lang=json */
            const string content = @"{""id"":1,""login"":""bot"",""name"":null,""email"":null,""avatar_url"":""https://github.com/a.jpg""}";
            var client = new GitHubClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.FirstName.Should().Be("bot");
            info.LastName.Should().BeEmpty();
            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void GitHub_Should_HandleMultiPartName()
        {
            /* lang=json */
            const string content = @"{""id"":1,""login"":""user"",""name"":""Mary Jane Watson"",""email"":null,""avatar_url"":""https://github.com/a.jpg""}";
            var client = new GitHubClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.FirstName.Should().Be("Mary");
            info.LastName.Should().Be("Watson");
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Google

        /* lang=json */
        private const string GoogleContent = @"{""id"":""g-123"",""email"":""user@gmail.com"",""given_name"":""John"",""family_name"":""Smith"",""picture"":""https://google.com/photo.jpg""}";

        [Test]
        public void Google_Should_ParseAndSerializeUserInfo()
        {
            var client = new GoogleClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(GoogleContent);

            info.Id.Should().Be("g-123");
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Smith");
            info.Email.Should().Be("user@gmail.com");
            info.AvatarUri.Small.Should().Contain("sz=36");
            info.AvatarUri.Normal.Should().Be("https://google.com/photo.jpg");
            info.AvatarUri.Large.Should().Contain("sz=300");

            AssertUserInfoSerializesCorrectly(info);
            AssertFieldRoundtrips(info, "Id", "g-123");
            AssertFieldRoundtrips(info, "Email", "user@gmail.com");
        }

        [Test]
        public void Google_Should_HandleMissingPicture()
        {
            /* lang=json */
            const string content = @"{""id"":""1"",""email"":""e@g.com"",""given_name"":""A"",""family_name"":""B""}";
            var client = new GoogleClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.AvatarUri.Small.Should().BeEmpty();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeEmpty();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Instagram

        /* lang=json */
        private const string InstagramTokenContent = @"{""access_token"":""token123"",""user"":{""id"":""ig-1"",""username"":""johndoe"",""full_name"":""John Doe"",""profile_picture"":""https://ig.com/pic.jpg""}}";

        [Test]
        public void Instagram_Should_ParseAndSerializeUserInfo()
        {
            var client = new InstagramClientDescendant(_factory, _configuration);
            client.SimulateAfterGetAccessToken(InstagramTokenContent);
            var info = client.ParseUserInfo(string.Empty);

            info.Id.Should().Be("ig-1");
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Doe");
            info.AvatarUri.Normal.Should().Be("https://ig.com/pic.jpg");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Instagram_Should_FallbackToUsername_WhenFullNameIsSingleWord()
        {
            /* lang=json */
            const string content = @"{""access_token"":""t"",""user"":{""id"":""1"",""username"":""madonna"",""full_name"":""Madonna"",""profile_picture"":""p.jpg""}}";
            var client = new InstagramClientDescendant(_factory, _configuration);
            client.SimulateAfterGetAccessToken(content);
            var info = client.ParseUserInfo(string.Empty);

            info.FirstName.Should().Be("Madonna");
            info.LastName.Should().BeEmpty();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region LinkedIn (XML)

        private const string LinkedInContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<person>
    <id>li-123</id>
    <first-name>Jane</first-name>
    <last-name>Doe</last-name>
    <email-address>jane@linkedin.com</email-address>
    <picture-url>https://linkedin.com/photo_80x80.jpg</picture-url>
</person>";

        [Test]
        public void LinkedIn_Should_ParseXmlAndSerializeUserInfo()
        {
            var client = new LinkedInClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(LinkedInContent);

            info.Id.Should().Be("li-123");
            info.FirstName.Should().Be("Jane");
            info.LastName.Should().Be("Doe");
            info.Email.Should().Be("jane@linkedin.com");
            info.AvatarUri.Normal.Should().Be("https://linkedin.com/photo_80x80.jpg");

            AssertUserInfoSerializesCorrectly(info);
            AssertFieldRoundtrips(info, "Id", "li-123");
        }

        [Test]
        public void LinkedIn_Should_UseDefaultAvatar_WhenPictureUrlIsMissing()
        {
            const string content = @"<?xml version=""1.0"" encoding=""UTF-8""?><person><id>1</id><first-name>A</first-name><last-name>B</last-name></person>";
            var client = new LinkedInClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.AvatarUri.Normal.Should().Contain("linkedin.com");
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region LoginCidadao

        /* lang=json */
        private const string LoginCidadaoContent = @"{""first_name"":""Carlos"",""last_name"":""Silva"",""cpf"":""123.456.789-00"",""email"":""carlos@gov.br""}";

        [Test]
        public void LoginCidadao_Should_ParseAndSerializeUserInfo()
        {
            var client = new LoginCidadaoClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(LoginCidadaoContent);

            info.FirstName.Should().Be("Carlos");
            info.LastName.Should().Be("Silva");
            info.Email.Should().Be("carlos@gov.br");
            info.Should().BeOfType<LoginCidadaoClient.Cidadao>();
            ((LoginCidadaoClient.Cidadao)info).Cpf.Should().Be("123.456.789-00");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void LoginCidadao_Should_SerializeCpfField()
        {
            var client = new LoginCidadaoClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(LoginCidadaoContent);

            var json = JsonSerializer.Serialize((LoginCidadaoClient.Cidadao)info, Options);
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("Cpf").GetString().Should().Be("123.456.789-00");
        }

        #endregion

        #region MailRu

        /* lang=json */
        private const string MailRuContent = @"[{""uid"":""mail-1"",""first_name"":""Ivan"",""last_name"":""Petrov"",""email"":""ivan@mail.ru"",""pic"":""https://mail.ru/pic.jpg""}]";

        [Test]
        public void MailRu_Should_ParseArrayAndSerializeUserInfo()
        {
            var client = new MailRuClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(MailRuContent);

            info.Id.Should().Be("mail-1");
            info.FirstName.Should().Be("Ivan");
            info.LastName.Should().Be("Petrov");
            info.Email.Should().Be("ivan@mail.ru");
            info.AvatarUri.Normal.Should().Be("https://mail.ru/pic.jpg");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void MailRu_Should_HandleNullEmail()
        {
            /* lang=json */
            const string content = @"[{""uid"":""1"",""first_name"":""A"",""last_name"":""B"",""pic"":""p.jpg""}]";
            var client = new MailRuClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.Email.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Odnoklassniki

        /* lang=json */
        private const string OdnoklassnikiContent = @"{""uid"":""ok-1"",""first_name"":""Olga"",""last_name"":""Ivanova"",""pic_1"":""https://ok.ru/pic.jpg?id=123&photoType=4""}";

        [Test]
        public void Odnoklassniki_Should_ParseAndSerializeUserInfo()
        {
            var client = new OdnoklassnikiClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(OdnoklassnikiContent);

            info.Id.Should().Be("ok-1");
            info.FirstName.Should().Be("Olga");
            info.LastName.Should().Be("Ivanova");
            info.AvatarUri.Normal.Should().Contain("photoType=4");
            info.AvatarUri.Large.Should().Contain("photoType=6");

            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Salesforce

        /* lang=json */
        private const string SalesforceContent = @"{""id"":""sf-123"",""email"":""admin@sf.com"",""first_name"":""Sales"",""last_name"":""Admin"",""photos"":{""thumbnail"":""https://sf.com/thumb.jpg"",""picture"":""https://sf.com/pic.jpg""}}";

        [Test]
        public void Salesforce_Should_ParseAndSerializeUserInfo()
        {
            var client = new SalesforceClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(SalesforceContent);

            info.Id.Should().Be("sf-123");
            info.FirstName.Should().Be("Sales");
            info.LastName.Should().Be("Admin");
            info.Email.Should().Be("admin@sf.com");
            info.AvatarUri.Small.Should().Be("https://sf.com/thumb.jpg");
            info.AvatarUri.Normal.Should().Be("https://sf.com/pic.jpg");

            AssertUserInfoSerializesCorrectly(info);
            AssertFieldRoundtrips(info, "Email", "admin@sf.com");
        }

        #endregion

        #region Spotify

        /* lang=json */
        private const string SpotifyContent = @"{""id"":""sp-1"",""display_name"":""DJ Cool"",""email"":""dj@spotify.com"",""images"":[{""url"":""https://spotify.com/img.jpg""}]}";

        [Test]
        public void Spotify_Should_ParseAndSerializeUserInfo()
        {
            var client = new SpotifyClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(SpotifyContent);

            info.Id.Should().Be("sp-1");
            info.FirstName.Should().Be("DJ Cool");
            info.Email.Should().Be("dj@spotify.com");
            info.ProviderName.Should().Be("Spotify");
            info.AvatarUri.Small.Should().Be("https://spotify.com/img.jpg");
            info.AvatarUri.Normal.Should().Be("https://spotify.com/img.jpg");
            info.AvatarUri.Large.Should().Be("https://spotify.com/img.jpg");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Spotify_Should_HandleMissingImages()
        {
            /* lang=json */
            const string content = @"{""id"":""1"",""display_name"":""Test"",""email"":""t@s.com""}";
            var client = new SpotifyClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.AvatarUri.Normal.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Todoist

        /* lang=json */
        private const string TodoistContent = @"{""User"":{""id"":1234,""email"":""user@todoist.com"",""full_name"":""Todo User"",""avatar_small"":""https://td.com/s.jpg"",""avatar_medium"":""https://td.com/m.jpg"",""avatar_big"":""https://td.com/l.jpg""}}";

        [Test]
        public void Todoist_Should_ParseAndSerializeUserInfo()
        {
            var client = new TodoistClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(TodoistContent);

            info.Id.Should().Be("1234");
            info.Email.Should().Be("user@todoist.com");
            info.LastName.Should().Be("Todo User");
            info.AvatarUri.Small.Should().Be("https://td.com/s.jpg");
            info.AvatarUri.Normal.Should().Be("https://td.com/m.jpg");
            info.AvatarUri.Large.Should().Be("https://td.com/l.jpg");

            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Twitter

        /* lang=json */
        private const string TwitterContent = @"{""id"":987,""name"":""Tweet Bird"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";

        [Test]
        public void Twitter_Should_ParseAndSerializeUserInfo()
        {
            var client = new TwitterClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(TwitterContent);

            info.Id.Should().Be("987");
            info.FirstName.Should().Be("Tweet");
            info.LastName.Should().Be("Bird");
            info.Email.Should().BeNull();
            info.AvatarUri.Small.Should().Contain("mini");
            info.AvatarUri.Normal.Should().Contain("normal");
            info.AvatarUri.Large.Should().Contain("bigger");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Twitter_Should_HandleSingleWordName()
        {
            /* lang=json */
            const string content = @"{""id"":1,""name"":""Cher"",""profile_image_url"":""https://twitter.com/pic_normal.jpg""}";
            var client = new TwitterClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.FirstName.Should().Be("Cher");
            info.LastName.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Uber

        /* lang=json */
        private const string UberContent = @"{""first_name"":""Uber"",""last_name"":""Rider"",""email"":""rider@uber.com"",""picture"":""https://uber.com/pic.jpg""}";

        [Test]
        public void Uber_Should_ParseAndSerializeUserInfo()
        {
            var client = new UberClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(UberContent);

            info.FirstName.Should().Be("Uber");
            info.LastName.Should().Be("Rider");
            info.Email.Should().Be("rider@uber.com");
            info.AvatarUri.Small.Should().Be("https://uber.com/pic.jpg");
            info.AvatarUri.Normal.Should().Be("https://uber.com/pic.jpg");
            info.AvatarUri.Large.Should().Be("https://uber.com/pic.jpg");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Uber_Should_HandleMissingFields()
        {
            /* lang=json */
            const string content = @"{""first_name"":""Test""}";
            var client = new UberClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.FirstName.Should().Be("Test");
            info.LastName.Should().BeNull();
            info.Email.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region VK (Vkontakte)

        /* lang=json */
        private const string VkContent = @"{""response"":[{""id"":456,""first_name"":""Vasily"",""last_name"":""Pupkin"",""has_photo"":true,""photo_max_orig"":""https://vk.com/photo.jpg""}]}";

        [Test]
        public void Vk_Should_ParseAndSerializeUserInfo()
        {
            var client = new VkClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(VkContent);

            info.Id.Should().Be("456");
            info.FirstName.Should().Be("Vasily");
            info.LastName.Should().Be("Pupkin");
            info.AvatarUri.Normal.Should().Be("https://vk.com/photo.jpg");

            AssertUserInfoSerializesCorrectly(info);
            AssertFieldRoundtrips(info, "Id", "456");
        }

        [Test]
        public void Vk_Should_HandleNoPhoto()
        {
            /* lang=json */
            const string content = @"{""response"":[{""id"":1,""first_name"":""A"",""last_name"":""B"",""has_photo"":false}]}";
            var client = new VkClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.AvatarUri.Normal.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region VSTS

        /* lang=json */
        private const string VSTSContent = @"{""id"":""vsts-guid-123"",""displayName"":""Dev User"",""emailAddress"":""dev@visualstudio.com""}";

        [Test]
        public void VSTS_Should_ParseAndSerializeUserInfo()
        {
            var client = new VSTSClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(VSTSContent);

            info.Id.Should().Be("vsts-guid-123");
            info.FirstName.Should().Be("Dev User");
            info.Email.Should().Be("dev@visualstudio.com");
            info.AvatarUri.Small.Should().Contain("vsts-guid-123").And.Contain("small");
            info.AvatarUri.Normal.Should().Contain("vsts-guid-123").And.Contain("medium");
            info.AvatarUri.Large.Should().Contain("vsts-guid-123").And.Contain("large");

            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region WindowsLive

        /* lang=json */
        private const string WindowsLiveContent = @"{""id"":""wl-123"",""first_name"":""Win"",""last_name"":""User"",""emails"":{""preferred"":""win@live.com""}}";

        [Test]
        public void WindowsLive_Should_ParseAndSerializeUserInfo()
        {
            _configuration.Scope.Returns("WL.EMAILS");
            var client = new WindowsLiveClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(WindowsLiveContent);

            info.Id.Should().Be("wl-123");
            info.FirstName.Should().Be("Win");
            info.LastName.Should().Be("User");
            info.Email.Should().Be("win@live.com");
            info.AvatarUri.Small.Should().Contain("wl-123");
            info.AvatarUri.Normal.Should().Contain("wl-123");
            info.AvatarUri.Large.Should().Contain("wl-123");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void WindowsLive_Should_NotParseEmail_WhenScopeDoesNotIncludeEmails()
        {
            _configuration.Scope.Returns("WL.BASIC");
            var client = new WindowsLiveClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(WindowsLiveContent);

            info.Email.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Xing

        /* lang=json */
        private const string XingContent = @"{""users"":[{""id"":""xing-1"",""first_name"":""Max"",""last_name"":""Mustermann"",""active_email"":""max@xing.com"",""photo_urls"":{""size_48x48"":""https://xing.com/48.jpg"",""size_128x128"":""https://xing.com/128.jpg"",""size_256x256"":""https://xing.com/256.jpg""}}]}";

        [Test]
        public void Xing_Should_ParseAndSerializeUserInfo()
        {
            var client = new XingClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(XingContent);

            info.Id.Should().Be("xing-1");
            info.FirstName.Should().Be("Max");
            info.LastName.Should().Be("Mustermann");
            info.Email.Should().Be("max@xing.com");
            info.AvatarUri.Small.Should().Be("https://xing.com/48.jpg");
            info.AvatarUri.Normal.Should().Be("https://xing.com/128.jpg");
            info.AvatarUri.Large.Should().Be("https://xing.com/256.jpg");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Xing_Should_HandleEmptyUsersArray()
        {
            /* lang=json */
            const string content = @"{""users"":[]}";
            var client = new XingClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.Id.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Xing_Should_HandleNullPhotoUrls()
        {
            /* lang=json */
            const string content = @"{""users"":[{""id"":""1"",""first_name"":""A"",""last_name"":""B"",""active_email"":""a@b.com"",""photo_urls"":null}]}";
            var client = new XingClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Normal.Should().BeNull();
            info.AvatarUri.Large.Should().BeNull();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Yandex

        /* lang=json */
        private const string YandexContent = @"{""id"":""ya-1"",""real_name"":""Yuri Gagarin"",""display_name"":""ygagarin"",""default_email"":""yuri@yandex.ru"",""default_avatar_id"":""avatar123""}";

        [Test]
        public void Yandex_Should_ParseAndSerializeUserInfo()
        {
            var client = new YandexClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(YandexContent);

            info.Id.Should().Be("ya-1");
            info.FirstName.Should().Be("Yuri");
            info.LastName.Should().Be("Gagarin");
            info.Email.Should().Be("yuri@yandex.ru");
            info.AvatarUri.Small.Should().Contain("avatar123");
            info.AvatarUri.Normal.Should().Contain("avatar123");
            info.AvatarUri.Large.Should().Contain("avatar123");

            AssertUserInfoSerializesCorrectly(info);
        }

        [Test]
        public void Yandex_Should_FallbackToDisplayName_WhenRealNameIsSingleWord()
        {
            /* lang=json */
            const string content = @"{""id"":""1"",""real_name"":""Madonna"",""display_name"":""md"",""default_email"":null,""default_avatar_id"":""""}";
            var client = new YandexClientDescendant(_factory, _configuration);
            var info = client.ParseUserInfo(content);

            info.FirstName.Should().Be("Madonna");
            info.LastName.Should().BeEmpty();
            AssertUserInfoSerializesCorrectly(info);
        }

        #endregion

        #region Cross-provider serialization consistency

        [Test]
        public void AllProviders_Should_ProduceConsistentJsonStructure()
        {
            var providers = new (string Name, UserInfo Info)[]
            {
                ("Asana", new AsanaClientDescendant(_factory, _configuration).ParseUserInfo(AsanaContent)),
                ("DigitalOcean", new DigitalOceanClientDescendant(_factory, _configuration).ParseUserInfo(DigitalOceanContent)),
                ("ExactOnline", new ExactOnlineClientDescendant(_factory, _configuration).ParseUserInfo(ExactOnlineContent)),
                ("Facebook", new FacebookClientDescendant(_factory, _configuration).ParseUserInfo(FacebookContent)),
                ("Fitbit", new FitbitClientDescendant(_factory, _configuration).ParseUserInfo(FitbitContent)),
                ("Foursquare", new FoursquareClientDescendant(_factory, _configuration).ParseUserInfo(FoursquareContent)),
                ("GitHub", new GitHubClientDescendant(_factory, _configuration).ParseUserInfo(GitHubContent)),
                ("Google", new GoogleClientDescendant(_factory, _configuration).ParseUserInfo(GoogleContent)),
                ("MailRu", new MailRuClientDescendant(_factory, _configuration).ParseUserInfo(MailRuContent)),
                ("Odnoklassniki", new OdnoklassnikiClientDescendant(_factory, _configuration).ParseUserInfo(OdnoklassnikiContent)),
                ("Salesforce", new SalesforceClientDescendant(_factory, _configuration).ParseUserInfo(SalesforceContent)),
                ("Spotify", new SpotifyClientDescendant(_factory, _configuration).ParseUserInfo(SpotifyContent)),
                ("Todoist", new TodoistClientDescendant(_factory, _configuration).ParseUserInfo(TodoistContent)),
                ("Twitter", new TwitterClientDescendant(_factory, _configuration).ParseUserInfo(TwitterContent)),
                ("Uber", new UberClientDescendant(_factory, _configuration).ParseUserInfo(UberContent)),
                ("Vk", new VkClientDescendant(_factory, _configuration).ParseUserInfo(VkContent)),
                ("VSTS", new VSTSClientDescendant(_factory, _configuration).ParseUserInfo(VSTSContent)),
                ("Xing", new XingClientDescendant(_factory, _configuration).ParseUserInfo(XingContent)),
                ("Yandex", new YandexClientDescendant(_factory, _configuration).ParseUserInfo(YandexContent)),
            };

            foreach (var (name, info) in providers)
            {
                var json = JsonSerializer.Serialize(info, Options);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                root.TryGetProperty("Id", out _).Should().BeTrue($"{name} should have Id property");
                root.TryGetProperty("FirstName", out _).Should().BeTrue($"{name} should have FirstName property");
                root.TryGetProperty("LastName", out _).Should().BeTrue($"{name} should have LastName property");
                root.TryGetProperty("Email", out _).Should().BeTrue($"{name} should have Email property");
                root.TryGetProperty("AvatarUri", out var avatar).Should().BeTrue($"{name} should have AvatarUri property");
                avatar.TryGetProperty("Small", out _).Should().BeTrue($"{name} should have AvatarUri.Small property");
                avatar.TryGetProperty("Normal", out _).Should().BeTrue($"{name} should have AvatarUri.Normal property");
                avatar.TryGetProperty("Large", out _).Should().BeTrue($"{name} should have AvatarUri.Large property");
            }
        }

        #endregion

        #region Descendant classes

        private class AsanaClientDescendant : AsanaClient
        {
            public AsanaClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class DigitalOceanClientDescendant : DigitalOceanClient
        {
            public DigitalOceanClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class ExactOnlineClientDescendant : ExactOnlineClient
        {
            public ExactOnlineClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class FacebookClientDescendant : FacebookClient
        {
            public FacebookClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class FitbitClientDescendant : FitbitClient
        {
            public FitbitClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class FoursquareClientDescendant : FoursquareClient
        {
            public FoursquareClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class GitHubClientDescendant : GitHubClient
        {
            public GitHubClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class GoogleClientDescendant : GoogleClient
        {
            public GoogleClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class InstagramClientDescendant : InstagramClient
        {
            public InstagramClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public void SimulateAfterGetAccessToken(string content)
            {
                AfterGetAccessToken(new BeforeAfterRequestArgs
                {
                    Response = new RestResponse { Content = content }
                });
            }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class LinkedInClientDescendant : LinkedInClient
        {
            public LinkedInClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class LoginCidadaoClientDescendant : LoginCidadaoClient
        {
            public LoginCidadaoClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class MailRuClientDescendant : MailRuClient
        {
            public MailRuClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class OdnoklassnikiClientDescendant : OdnoklassnikiClient
        {
            public OdnoklassnikiClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class SalesforceClientDescendant : SalesforceClient
        {
            public SalesforceClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class SpotifyClientDescendant : SpotifyClient
        {
            public SpotifyClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class TodoistClientDescendant : TodoistClient
        {
            public TodoistClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class TwitterClientDescendant : TwitterClient
        {
            public TwitterClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class UberClientDescendant : UberClient
        {
            public UberClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class VkClientDescendant : VkClient
        {
            public VkClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class VSTSClientDescendant : VSTSClient
        {
            public VSTSClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class WindowsLiveClientDescendant : WindowsLiveClient
        {
            public WindowsLiveClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class XingClientDescendant : XingClient
        {
            public XingClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        private class YandexClientDescendant : YandexClient
        {
            public YandexClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }
            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }

        #endregion
    }
}
