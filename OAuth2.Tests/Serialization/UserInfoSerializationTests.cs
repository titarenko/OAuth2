using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using OAuth2.Models;

namespace OAuth2.Tests.Serialization
{
    [TestFixture]
    public class UserInfoSerializationTests
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = null, // PascalCase to match C# property names
            WriteIndented = false
        };

        [Test]
        public void Should_SerializeAllFields()
        {
            var userInfo = CreateFullUserInfo();
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("Id").GetString().Should().Be("user-123");
            root.GetProperty("ProviderName").GetString().Should().Be("TestProvider");
            root.GetProperty("Email").GetString().Should().Be("test@example.com");
            root.GetProperty("FirstName").GetString().Should().Be("John");
            root.GetProperty("LastName").GetString().Should().Be("Doe");
            root.GetProperty("PhotoUri").GetString().Should().Be("https://example.com/photo_normal.jpg");
        }

        [Test]
        public void Should_SerializeAvatarUriAsNestedObject()
        {
            var userInfo = CreateFullUserInfo();
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            avatar.GetProperty("Small").GetString().Should().Be("https://example.com/photo_small.jpg");
            avatar.GetProperty("Normal").GetString().Should().Be("https://example.com/photo_normal.jpg");
            avatar.GetProperty("Large").GetString().Should().Be("https://example.com/photo_large.jpg");
        }

        [Test]
        public void Should_SerializeNullFieldsAsNull()
        {
            var userInfo = new UserInfo { Id = "1" };
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("Email").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("FirstName").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("LastName").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Test]
        public void Should_SerializeEmptyStringsAsEmptyStrings()
        {
            var userInfo = new UserInfo
            {
                Id = "1",
                Email = "",
                FirstName = "",
                LastName = ""
            };
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("Email").GetString().Should().BeEmpty();
            root.GetProperty("FirstName").GetString().Should().BeEmpty();
            root.GetProperty("LastName").GetString().Should().BeEmpty();
        }

        [Test]
        public void Should_HaveCorrectPropertyNames()
        {
            var userInfo = CreateFullUserInfo();
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Verify PascalCase property names
            root.TryGetProperty("Id", out _).Should().BeTrue();
            root.TryGetProperty("ProviderName", out _).Should().BeTrue();
            root.TryGetProperty("Email", out _).Should().BeTrue();
            root.TryGetProperty("FirstName", out _).Should().BeTrue();
            root.TryGetProperty("LastName", out _).Should().BeTrue();
            root.TryGetProperty("PhotoUri", out _).Should().BeTrue();
            root.TryGetProperty("AvatarUri", out _).Should().BeTrue();

            // Verify no camelCase or snake_case
            root.TryGetProperty("id", out _).Should().BeFalse();
            root.TryGetProperty("provider_name", out _).Should().BeFalse();
            root.TryGetProperty("firstName", out _).Should().BeFalse();
        }

        [Test]
        public void Should_SerializeDefaultUserInfo_WithEmptyAvatarUri()
        {
            var userInfo = new UserInfo();
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            avatar.GetProperty("Small").ValueKind.Should().Be(JsonValueKind.Null);
            avatar.GetProperty("Normal").ValueKind.Should().Be(JsonValueKind.Null);
            avatar.GetProperty("Large").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Test]
        public void Should_SerializePhotoUri_MatchingAvatarUriNormal()
        {
            var userInfo = new UserInfo
            {
                AvatarUri = { Normal = "https://example.com/pic.jpg" }
            };
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);

            doc.RootElement.GetProperty("PhotoUri").GetString().Should().Be("https://example.com/pic.jpg");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString()
                .Should().Be("https://example.com/pic.jpg");
        }

        [Test]
        public void Should_SerializeAvatarInfo_AllFields()
        {
            var avatar = new AvatarInfo
            {
                Small = "small.jpg",
                Normal = "normal.jpg",
                Large = "large.jpg"
            };
            var json = JsonSerializer.Serialize(avatar, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("Small").GetString().Should().Be("small.jpg");
            root.GetProperty("Normal").GetString().Should().Be("normal.jpg");
            root.GetProperty("Large").GetString().Should().Be("large.jpg");
        }

        [Test]
        public void Should_DeserializeAvatarInfo_Roundtrip()
        {
            var original = new AvatarInfo
            {
                Small = "small.jpg",
                Normal = "normal.jpg",
                Large = "large.jpg"
            };
            var json = JsonSerializer.Serialize(original, Options);
            var deserialized = JsonSerializer.Deserialize<AvatarInfo>(json, Options);

            deserialized.Small.Should().Be(original.Small);
            deserialized.Normal.Should().Be(original.Normal);
            deserialized.Large.Should().Be(original.Large);
        }

        [Test]
        public void Should_ProduceStableJsonOutput()
        {
            var userInfo = CreateFullUserInfo();
            var json1 = JsonSerializer.Serialize(userInfo, Options);
            var json2 = JsonSerializer.Serialize(userInfo, Options);

            json1.Should().Be(json2);
        }

        [Test]
        public void Should_HandleSpecialCharactersInFields()
        {
            var userInfo = new UserInfo
            {
                Id = "id-with-special/chars",
                FirstName = "José",
                LastName = "O'Brien",
                Email = "user+tag@example.com"
            };
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("Id").GetString().Should().Be("id-with-special/chars");
            root.GetProperty("FirstName").GetString().Should().Be("José");
            root.GetProperty("LastName").GetString().Should().Be("O'Brien");
            root.GetProperty("Email").GetString().Should().Be("user+tag@example.com");
        }

        [Test]
        public void Should_HandleUnicodeInAvatarUrls()
        {
            var userInfo = new UserInfo
            {
                AvatarUri =
                {
                    Small = "https://example.com/pic?name=José",
                    Normal = "https://example.com/pic?name=José",
                    Large = "https://example.com/pic?name=José"
                }
            };
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            avatar.GetProperty("Small").GetString().Should().Contain("José");
        }

        private static UserInfo CreateFullUserInfo()
        {
            return new UserInfo
            {
                Id = "user-123",
                ProviderName = "TestProvider",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                AvatarUri =
                {
                    Small = "https://example.com/photo_small.jpg",
                    Normal = "https://example.com/photo_normal.jpg",
                    Large = "https://example.com/photo_large.jpg"
                }
            };
        }
    }
}
