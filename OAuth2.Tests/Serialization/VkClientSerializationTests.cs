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
    public class VkClientSerializationTests
    {
        private IRequestFactory _factory = null!;
        private IClientConfiguration _configuration = null!;
        private TestableVkClient _client = null!;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _configuration = Substitute.For<IClientConfiguration>();
            _client = new TestableVkClient(_factory, _configuration);
        }

        [Test]
        public void ParseUserInfo_ValidContent_ReturnsCorrectFields()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":[{""id"":456,""first_name"":""Vasily"",""last_name"":""Pupkin"",""has_photo"":true,""photo_max_orig"":""https://vk.com/photo.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("456");
            info.FirstName.Should().Be("Vasily");
            info.LastName.Should().Be("Pupkin");
        }

        [Test]
        public void ParseUserInfo_HasPhotoTrue_SetsNormalAvatar()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":[{""id"":1,""first_name"":""A"",""last_name"":""B"",""has_photo"":true,""photo_max_orig"":""https://vk.com/photo.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Be("https://vk.com/photo.jpg");
            info.AvatarUri.Small.Should().BeNull();
            info.AvatarUri.Large.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_HasPhotoFalse_AvatarIsNull()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":[{""id"":1,""first_name"":""A"",""last_name"":""B"",""has_photo"":false}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_HasPhotoAsNumericOne_TreatsAsTrue()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":[{""id"":1,""first_name"":""A"",""last_name"":""B"",""has_photo"":1,""photo_max_orig"":""https://vk.com/photo.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().Be("https://vk.com/photo.jpg");
        }

        [Test]
        public void ParseUserInfo_HasPhotoAsNumericZero_TreatsAsFalse()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":[{""id"":1,""first_name"":""A"",""last_name"":""B"",""has_photo"":0}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.AvatarUri.Normal.Should().BeNull();
        }

        [Test]
        public void ParseUserInfo_NumericId_ConvertsToString()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":[{""id"":99999,""first_name"":""A"",""last_name"":""B"",""has_photo"":false}]}";

            // act
            var info = _client.ParseUserInfo(content);

            // assert
            info.Id.Should().Be("99999");
        }

        [Test]
        public void ParseUserInfo_ValidContent_SerializesToValidJson()
        {
            // arrange
            /* lang=json */
            const string content = @"{""response"":[{""id"":456,""first_name"":""Vasily"",""last_name"":""Pupkin"",""has_photo"":true,""photo_max_orig"":""https://vk.com/photo.jpg""}]}";

            // act
            var info = _client.ParseUserInfo(content);
            var json = JsonSerializer.Serialize(info);
            using var doc = JsonDocument.Parse(json);

            // assert
            doc.RootElement.GetProperty("Id").GetString().Should().Be("456");
            doc.RootElement.GetProperty("FirstName").GetString().Should().Be("Vasily");
        }

        private class TestableVkClient : VkClient
        {
            public TestableVkClient(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration) { }

            public new UserInfo ParseUserInfo(string content) => base.ParseUserInfo(content);
        }
    }
}
