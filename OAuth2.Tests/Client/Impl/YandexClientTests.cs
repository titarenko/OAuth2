using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;

namespace OAuth2.Tests.Client.Impl
{
    public class YandexClientTests
    {
        private const string Content = "{\"id\": \"349\", \"login\": \"mylogin\", \"client_id\": \"e0000000000000000000191f3280bb\", \"display_name\": \"My Name\", \"real_name\": \"Real Name\", \"first_name\": \"Real\", \"last_name\": \"Name\", \"default_email\": \"mymail@yandex.ru\", \"emails\": [\"mymail@yandex.ru\"], \"default_avatar_id\": \"\", \"is_avatar_empty\": true, \"psuid\": \"1.AA.XXXXXXXXXXXXXXXXXXXXXXX.YYYYYYYYYYYYYYYYYYYYYYYYY\"}";
        private const string ContentWithAvatar = "{\"id\": \"349\", \"login\": \"mylogin\", \"client_id\": \"e0000000000000000000191f3280bb\", \"display_name\": \"My Name\", \"real_name\": \"Real Name\", \"first_name\": \"Real\", \"last_name\": \"Name\", \"default_email\": \"mymail@yandex.ru\", \"emails\": [\"mymail@yandex.ru\"], \"default_avatar_id\": \"1111/enc-000\", \"is_avatar_empty\": false, \"psuid\": \"1.AA.XXXXXXXXXXXXXXXXXXXXXXX.YYYYYYYYYYYYYYYYYYYYYYYYY\"}";

        private YandexClientDescendant _descendant;
        private IRequestFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = Substitute.For<IRequestFactory>();
            _descendant = new YandexClientDescendant(
                _factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectAccessCodeServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessCodeServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.yandex.ru");
            endpoint.Resource.Should().Be("/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://oauth.yandex.ru");
            endpoint.Resource.Should().Be("/token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://login.yandex.ru");
            endpoint.Resource.Should().Be("/info");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("349");
			info.AvatarUri.Normal.Should().BeNull();
        }

        [Test]
        public void Should_AvatarOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = _descendant.ParseUserInfo(ContentWithAvatar);

            // assert
            info.Id.Should().Be("349");
			info.AvatarUri.Normal.Should().Be("https://avatars.yandex.net/get-yapic/1111/enc-000/islands-retina-50");
		}

        private class YandexClientDescendant : YandexClient
        {
            public YandexClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
                : base(factory, configuration)
            {
            }

            public Endpoint GetAccessCodeServiceEndpoint()
            {
                return AccessCodeServiceEndpoint;
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