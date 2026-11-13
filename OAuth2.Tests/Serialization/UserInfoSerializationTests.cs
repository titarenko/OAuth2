using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public void Deserialize_MissingOptionalClaims_DefaultsToNull()
        {
            // Arrange
            // Serialized library DTO, not a provider API response.
            /* lang=json */
            const string json = """{"Id":"user-123","ProviderName":"Google","Email":"test@example.com","FirstName":"John","LastName":"Doe"}""";

            // Act
            var info = JsonSerializer.Deserialize<UserInfo>(json, Options);

            // Assert
            info.Should().NotBeNull();
            info!.Id.Should().Be("user-123");
            info.ProviderName.Should().Be("Google");
            info.Email.Should().Be("test@example.com");
            info.FirstName.Should().Be("John");
            info.LastName.Should().Be("Doe");
            info.EmailVerified.Should().BeNull();
            info.ProviderData.Should().BeNull();
        }

        [Test]
        public void Roundtrip_AvatarInfo_DeserializesToEquivalentObject()
        {
            // Arrange
            var original = new AvatarInfo { Small = "small.jpg", Normal = "normal.jpg", Large = "large.jpg" };

            // Act
            var json = JsonSerializer.Serialize(original, Options);
            var deserialized = JsonSerializer.Deserialize<AvatarInfo>(json, Options);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Small.Should().Be(original.Small);
            deserialized.Normal.Should().Be(original.Normal);
            deserialized.Large.Should().Be(original.Large);
        }

        [Test]
        public void Roundtrip_CamelCaseNamingPolicy_PreservesClaims()
        {
            // Arrange
            var options = new JsonSerializerOptions(Options) { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var original = new UserInfo
            {
                ProviderName = "Google",
                EmailVerified = true,
                ProviderData = new Dictionary<string, string> { ["hd"] = "example.com" }
            };

            // Act
            var json = JsonSerializer.Serialize(original, options);
            using var doc = JsonDocument.Parse(json);
            var deserialized = JsonSerializer.Deserialize<UserInfo>(json, options);

            // Assert
            doc.RootElement.GetProperty("emailVerified").GetBoolean().Should().BeTrue();
            doc.RootElement.GetProperty("providerData").GetProperty("hd").GetString().Should().Be("example.com");
            deserialized.Should().NotBeNull();
            deserialized!.ProviderName.Should().Be("Google");
            deserialized.EmailVerified.Should().BeTrue();
            deserialized.ProviderData.Should().Contain("hd", "example.com");
        }

        [TestCase(JsonIgnoreCondition.WhenWritingNull)]
        [TestCase(JsonIgnoreCondition.WhenWritingDefault)]
        public void Roundtrip_FalseVerificationWithIgnoreCondition_PreservesFalse(JsonIgnoreCondition ignoreCondition)
        {
            // Arrange
            var options = new JsonSerializerOptions(Options) { DefaultIgnoreCondition = ignoreCondition };
            var original = new UserInfo { EmailVerified = false };

            // Act
            var json = JsonSerializer.Serialize(original, options);
            using var doc = JsonDocument.Parse(json);
            var deserialized = JsonSerializer.Deserialize<UserInfo>(json, options);

            // Assert
            doc.RootElement.GetProperty("EmailVerified").GetBoolean().Should().BeFalse();
            doc.RootElement.TryGetProperty("ProviderData", out _).Should().BeFalse();
            deserialized.Should().NotBeNull();
            deserialized!.EmailVerified.Should().BeFalse();
            deserialized.ProviderData.Should().BeNull();
        }

        [TestCase(true)]
        [TestCase(false)]
        [TestCase(null)]
        public void Roundtrip_UserInfoClaims_PreservesVerificationAndProviderData(bool? verified)
        {
            // Arrange
            var original = new UserInfo
            {
                Id = "user-123",
                ProviderName = "Google",
                Email = "test@example.com",
                EmailVerified = verified,
                ProviderData = new Dictionary<string, string> { ["hd"] = "example.com" }
            };

            // Act
            var json = JsonSerializer.Serialize(original, Options);
            using var doc = JsonDocument.Parse(json);
            var deserialized = JsonSerializer.Deserialize<UserInfo>(json, Options);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Id.Should().Be(original.Id);
            deserialized.ProviderName.Should().Be(original.ProviderName);
            deserialized.Email.Should().Be(original.Email);
            deserialized.EmailVerified.Should().Be(verified);
            deserialized.ProviderData.Should().ContainSingle();
            deserialized.ProviderData.Should().Contain("hd", "example.com");
            doc.RootElement.GetProperty("ProviderData").GetProperty("hd").GetString().Should().Be("example.com");
            doc.RootElement.TryGetProperty("HostedDomain", out _).Should().BeFalse();
        }

        [Test]
        public void Serialize_AvatarInfo_ContainsAllSizeFields()
        {
            // Arrange
            var avatar = new AvatarInfo { Small = "small.jpg", Normal = "normal.jpg", Large = "large.jpg" };

            // Act
            var json = JsonSerializer.Serialize(avatar, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            root.GetProperty("Small").GetString().Should().Be("small.jpg");
            root.GetProperty("Normal").GetString().Should().Be("normal.jpg");
            root.GetProperty("Large").GetString().Should().Be("large.jpg");
        }

        [Test]
        public void Serialize_DefaultUserInfo_ContainsNullAvatarFields()
        {
            // Arrange
            var userInfo = new UserInfo();

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            // Assert
            avatar.GetProperty("Small").ValueKind.Should().Be(JsonValueKind.Null);
            avatar.GetProperty("Normal").ValueKind.Should().Be(JsonValueKind.Null);
            avatar.GetProperty("Large").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Test]
        public void Serialize_FullyPopulatedUserInfo_ContainsAllFieldValues()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                Id = "user-123",
                ProviderName = "TestProvider",
                Email = "test@example.com",
                EmailVerified = true,
                ProviderData = new Dictionary<string, string> { ["custom"] = "value" },
                FirstName = "John",
                LastName = "Doe",
                AvatarUri =
                {
                    Small = "https://example.com/photo_small.jpg",
                    Normal = "https://example.com/photo_normal.jpg",
                    Large = "https://example.com/photo_large.jpg"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            root.GetProperty("Id").GetString().Should().Be("user-123");
            root.GetProperty("ProviderName").GetString().Should().Be("TestProvider");
            root.GetProperty("Email").GetString().Should().Be("test@example.com");
            root.GetProperty("EmailVerified").GetBoolean().Should().BeTrue();
            root.GetProperty("ProviderData").GetProperty("custom").GetString().Should().Be("value");
            root.GetProperty("FirstName").GetString().Should().Be("John");
            root.GetProperty("LastName").GetString().Should().Be("Doe");
            root.GetProperty("PhotoUri").GetString().Should().Be("https://example.com/photo_normal.jpg");
        }

        [Test]
        public void Serialize_FullyPopulatedUserInfo_ContainsNestedAvatarUri()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                AvatarUri =
                {
                    Small = "https://example.com/photo_small.jpg",
                    Normal = "https://example.com/photo_normal.jpg",
                    Large = "https://example.com/photo_large.jpg"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            // Assert
            avatar.GetProperty("Small").GetString().Should().Be("https://example.com/photo_small.jpg");
            avatar.GetProperty("Normal").GetString().Should().Be("https://example.com/photo_normal.jpg");
            avatar.GetProperty("Large").GetString().Should().Be("https://example.com/photo_large.jpg");
        }

        [TestCase(JsonIgnoreCondition.WhenWritingNull)]
        [TestCase(JsonIgnoreCondition.WhenWritingDefault)]
        public void Serialize_NullClaimsWithIgnoreCondition_OmitsClaims(JsonIgnoreCondition ignoreCondition)
        {
            // Arrange
            var options = new JsonSerializerOptions(Options) { DefaultIgnoreCondition = ignoreCondition };
            var userInfo = new UserInfo { Id = "user-123" };

            // Act
            var json = JsonSerializer.Serialize(userInfo, options);
            using var doc = JsonDocument.Parse(json);

            // Assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("user-123");
            doc.RootElement.TryGetProperty("EmailVerified", out _).Should().BeFalse();
            doc.RootElement.TryGetProperty("ProviderData", out _).Should().BeFalse();
        }

        [Test]
        public void Serialize_SameUserInfoTwice_ProducesIdenticalOutput()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                Id = "user-123",
                ProviderName = "TestProvider",
                Email = "test@example.com",
                EmailVerified = true,
                ProviderData = new Dictionary<string, string> { ["custom"] = "value" },
                FirstName = "John",
                LastName = "Doe",
                AvatarUri =
                {
                    Small = "https://example.com/photo_small.jpg",
                    Normal = "https://example.com/photo_normal.jpg",
                    Large = "https://example.com/photo_large.jpg"
                }
            };

            // Act
            var json1 = JsonSerializer.Serialize(userInfo, Options);
            var json2 = JsonSerializer.Serialize(userInfo, Options);

            // Assert
            json1.Should().Be(json2);
        }

        [Test]
        public void Serialize_UserInfo_UsesPascalCasePropertyNames()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                Id = "1",
                ProviderName = "Test",
                Email = "e@example.com",
                FirstName = "F",
                LastName = "L",
                AvatarUri = { Normal = "https://example.com/pic.jpg" }
            };

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            root.TryGetProperty("Id", out _).Should().BeTrue();
            root.TryGetProperty("ProviderName", out _).Should().BeTrue();
            root.TryGetProperty("Email", out _).Should().BeTrue();
            root.TryGetProperty("EmailVerified", out _).Should().BeTrue();
            root.TryGetProperty("ProviderData", out _).Should().BeTrue();
            root.TryGetProperty("FirstName", out _).Should().BeTrue();
            root.TryGetProperty("LastName", out _).Should().BeTrue();
            root.TryGetProperty("PhotoUri", out _).Should().BeTrue();
            root.TryGetProperty("AvatarUri", out _).Should().BeTrue();
            root.TryGetProperty("id", out _).Should().BeFalse();
            root.TryGetProperty("provider_name", out _).Should().BeFalse();
            root.TryGetProperty("firstName", out _).Should().BeFalse();
        }

        [Test]
        public void Serialize_UserInfoWithEmptyStrings_SerializesAsEmptyStrings()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "1", Email = "", FirstName = "", LastName = "" };

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            root.GetProperty("Email").GetString().Should().BeEmpty();
            root.GetProperty("FirstName").GetString().Should().BeEmpty();
            root.GetProperty("LastName").GetString().Should().BeEmpty();
        }

        [Test]
        public void Serialize_UserInfoWithNormalAvatar_PhotoUriMatchesAvatarUriNormal()
        {
            // Arrange
            var userInfo = new UserInfo { AvatarUri = { Normal = "https://example.com/pic.jpg" } };

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);

            // Assert
            doc.RootElement.GetProperty("PhotoUri").GetString().Should().Be("https://example.com/pic.jpg");
            doc.RootElement.GetProperty("AvatarUri").GetProperty("Normal").GetString().Should().Be("https://example.com/pic.jpg");
        }

        [Test]
        public void Serialize_UserInfoWithNullFields_SerializesAsJsonNull()
        {
            // Arrange
            var userInfo = new UserInfo { Id = "1" };

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            root.GetProperty("Email").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("EmailVerified").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("ProviderData").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("FirstName").ValueKind.Should().Be(JsonValueKind.Null);
            root.GetProperty("LastName").ValueKind.Should().Be(JsonValueKind.Null);
        }

        [Test]
        public void Serialize_UserInfoWithSpecialCharacters_PreservesCharacters()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                Id = "id-with-special/chars",
                FirstName = "José",
                LastName = "O'Brien",
                Email = "user+tag@example.com"
            };

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert
            root.GetProperty("Id").GetString().Should().Be("id-with-special/chars");
            root.GetProperty("FirstName").GetString().Should().Be("José");
            root.GetProperty("LastName").GetString().Should().Be("O'Brien");
            root.GetProperty("Email").GetString().Should().Be("user+tag@example.com");
        }

        [Test]
        public void Serialize_UserInfoWithUnicodeAvatarUrls_PreservesUnicode()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                AvatarUri =
                {
                    Small = "https://example.com/pic?name=José",
                    Normal = "https://example.com/pic?name=José",
                    Large = "https://example.com/pic?name=José"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(userInfo, Options);
            using var doc = JsonDocument.Parse(json);
            var avatar = doc.RootElement.GetProperty("AvatarUri");

            // Assert
            avatar.GetProperty("Small").GetString().Should().Contain("José");
        }
    }
}
