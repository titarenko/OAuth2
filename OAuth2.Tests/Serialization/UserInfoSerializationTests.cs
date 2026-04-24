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
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        [Test]
        public void Serialize_FullyPopulatedUserInfo_ContainsAllFieldValues()
        {
            // arrange
            var userInfo = new UserInfo
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

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // assert
            root.GetProperty("Id").GetString().Should().Be("user-123");
            root.GetProperty("ProviderName").GetString().Should().Be("TestProvider");
            root.GetProperty("Email").GetString().Should().Be("test@example.com");
            root.GetProperty("FirstName").GetString().Should().Be("John");
            root.GetProperty("LastName").GetString().Should().Be("Doe");
            root.GetProperty("PhotoUri").GetString().Should().Be("https://example.com/photo_normal.jpg");
        }

        [Test]
        public void Serialize_FullyPopulatedUserInfo_ContainsNestedAvatarUri()
        {
            // arrange
            var userInfo = new UserInfo
            {
                AvatarUri =
                {
                    Small = "https://example.com/photo_small.jpg",
                    Normal = "https://example.com/photo_normal.jpg",
                    Large = "https://example.com/photo_large.jpg"
                }
            };

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            // assert
            avatar.GetProperty("Small").GetString().Should().Be("https://example.com/photo_small.jpg");
            avatar.GetProperty("Normal").GetString().Should().Be("https://example.com/photo_normal.jpg");
            avatar.GetProperty("Large").GetString().Should().Be("https://example.com/photo_large.jpg");
        }

        [Test]
        public void Serialize_UserInfoWithNullFields_SerializesAsJsonNull()
        {
            // arrange
            var userInfo = new UserInfo { Id = "1" };

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // assert
            root.GetProperty("Email").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("FirstName").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("LastName").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Test]
        public void Serialize_UserInfoWithEmptyStrings_SerializesAsEmptyStrings()
        {
            // arrange
            var userInfo = new UserInfo
            {
                Id = "1",
                Email = "",
                FirstName = "",
                LastName = ""
            };

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // assert
            root.GetProperty("Email").GetString().Should().BeEmpty();
            root.GetProperty("FirstName").GetString().Should().BeEmpty();
            root.GetProperty("LastName").GetString().Should().BeEmpty();
        }

        [Test]
        public void Serialize_UserInfo_UsesPascalCasePropertyNames()
        {
            // arrange
            var userInfo = new UserInfo
            {
                Id = "1",
                ProviderName = "Test",
                Email = "e@t.com",
                FirstName = "F",
                LastName = "L",
                AvatarUri = { Normal = "http://test.com/pic.jpg" }
            };

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // assert
            root.TryGetProperty("Id", out _).Should().BeTrue();
            root.TryGetProperty("ProviderName", out _).Should().BeTrue();
            root.TryGetProperty("Email", out _).Should().BeTrue();
            root.TryGetProperty("FirstName", out _).Should().BeTrue();
            root.TryGetProperty("LastName", out _).Should().BeTrue();
            root.TryGetProperty("PhotoUri", out _).Should().BeTrue();
            root.TryGetProperty("AvatarUri", out _).Should().BeTrue();
            root.TryGetProperty("id", out _).Should().BeFalse();
            root.TryGetProperty("provider_name", out _).Should().BeFalse();
            root.TryGetProperty("firstName", out _).Should().BeFalse();
        }

        [Test]
        public void Serialize_DefaultUserInfo_ContainsNullAvatarFields()
        {
            // arrange
            var userInfo = new UserInfo();

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            // assert
            avatar.GetProperty("Small").ValueKind.Should().Be(JsonValueKind.Null);
            avatar.GetProperty("Normal").ValueKind.Should().Be(JsonValueKind.Null);
            avatar.GetProperty("Large").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Test]
        public void Serialize_UserInfoWithNormalAvatar_PhotoUriMatchesAvatarUriNormal()
        {
            // arrange
            var userInfo = new UserInfo
            {
                AvatarUri = { Normal = "https://example.com/pic.jpg" }
            };

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("PhotoUri").GetString().Should().Be("https://example.com/pic.jpg");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString()
                .Should().Be("https://example.com/pic.jpg");
        }

        [Test]
        public void Serialize_AvatarInfo_ContainsAllSizeFields()
        {
            // arrange
            var avatar = new AvatarInfo
            {
                Small = "small.jpg",
                Normal = "normal.jpg",
                Large = "large.jpg"
            };

            // act
            var json = JsonSerializer.Serialize(avatar, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // assert
            root.GetProperty("Small").GetString().Should().Be("small.jpg");
            root.GetProperty("Normal").GetString().Should().Be("normal.jpg");
            root.GetProperty("Large").GetString().Should().Be("large.jpg");
        }

        [Test]
        public void Roundtrip_AvatarInfo_DeserializesToEquivalentObject()
        {
            // arrange
            var original = new AvatarInfo
            {
                Small = "small.jpg",
                Normal = "normal.jpg",
                Large = "large.jpg"
            };

            // act
            var json = JsonSerializer.Serialize(original, Options);
            var deserialized = JsonSerializer.Deserialize<AvatarInfo>(json, Options);

            // assert
            deserialized.Should().NotBeNull();
            deserialized!.Small.Should().Be(original.Small);
            deserialized.Normal.Should().Be(original.Normal);
            deserialized.Large.Should().Be(original.Large);
        }

        [Test]
        public void Serialize_SameUserInfoTwice_ProducesIdenticalOutput()
        {
            // arrange
            var userInfo = new UserInfo
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

            // act
            var json1 = JsonSerializer.Serialize(userInfo, Options);
            var json2 = JsonSerializer.Serialize(userInfo, Options);

            // assert
            json1.Should().Be(json2);
        }

        [Test]
        public void Serialize_UserInfoWithSpecialCharacters_PreservesCharacters()
        {
            // arrange
            var userInfo = new UserInfo
            {
                Id = "id-with-special/chars",
                FirstName = "José",
                LastName = "O'Brien",
                Email = "user+tag@example.com"
            };

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // assert
            root.GetProperty("Id").GetString().Should().Be("id-with-special/chars");
            root.GetProperty("FirstName").GetString().Should().Be("José");
            root.GetProperty("LastName").GetString().Should().Be("O'Brien");
            root.GetProperty("Email").GetString().Should().Be("user+tag@example.com");
        }

        [Test]
        public void Serialize_UserInfoWithUnicodeAvatarUrls_PreservesUnicode()
        {
            // arrange
            var userInfo = new UserInfo
            {
                AvatarUri =
                {
                    Small = "https://example.com/pic?name=José",
                    Normal = "https://example.com/pic?name=José",
                    Large = "https://example.com/pic?name=José"
                }
            };

            // act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            // assert
            avatar.GetProperty("Small").GetString().Should().Contain("José");
        }
    }
}
