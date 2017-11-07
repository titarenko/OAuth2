using NSubstitute;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using RestSharp;
using FluentAssertions;

namespace OAuth2.Tests.Client.Impl
{

    [TestFixture]
    public class XingTests
    {
        private const string Content = "{\"users\":[{\"id\":\"123456_abcdef\",\"first_name\":\"Max\",\"last_name\":\"Mustermann\",\"display_name\":\"Max Mustermann\",\"page_name\":\"Max_Mustermann\",\"permalink\":\"https://www.xing.com/profile/Max_Mustermann\",\"employment_status\":\"EMPLOYEE\",\"gender\":\"m\",\"birth_date\":{\"day\":12,\"month\":8,\"year\":1963},\"active_email\":\"max.mustermann@xing.com\",\"time_zone\":{\"name\":\"Europe/Copenhagen\",\"utc_offset\":2.0},\"premium_services\":[\"SEARCH\",\"PRIVATEMESSAGES\"],\"badges\":[\"PREMIUM\",\"MODERATOR\"],\"wants\":\"einen neuen Job\",\"haves\":\"viele tolle Skills\",\"interests\":\"Flitzebogen schießen and so on\",\"organisation_member\":\"ACM, GI\",\"languages\":{\"de\":\"NATIVE\",\"en\":\"FLUENT\",\"fr\":null,\"zh\":\"BASIC\"},\"private_address\":{\"city\":\"Hamburg\",\"country\":\"DE\",\"zip_code\":\"20357\",\"street\":\"Privatstraße 1\",\"phone\":\"49|40|1234560\",\"fax\":\"||\",\"province\":\"Hamburg\",\"email\":\"max@mustermann.de\",\"mobile_phone\":\"49|0155|1234567\"},\"business_address\":{\"city\":\"Hamburg\",\"country\":\"DE\",\"zip_code\":\"20357\",\"street\":\"Geschäftsstraße 1a\",\"phone\":\"49|40|1234569\",\"fax\":\"49|40|1234561\",\"province\":\"Hamburg\",\"email\":\"max.mustermann@xing.com\",\"mobile_phone\":\"49|160|66666661\"},\"web_profiles\":{\"qype\":[\"http://qype.de/users/foo\"],\"google_plus\":[\"http://plus.google.com/foo\"],\"blog\":[\"http://blog.example.org\"],\"homepage\":[\"http://example.org\",\"http://other-example.org\"]},\"instant_messaging_accounts\":{\"skype\":\"1122334455\",\"googletalk\":\"max.mustermann\"},\"professional_experience\":{\"primary_company\":{\"id\":\"1_abcdef\",\"name\":\"XING AG\",\"title\":\"Softwareentwickler\",\"company_size\":\"201-500\",\"tag\":null,\"url\":\"http://www.xing.com\",\"career_level\":\"PROFESSIONAL_EXPERIENCED\",\"begin_date\":\"2010-01\",\"description\":null,\"end_date\":null,\"industry\":\"AEROSPACE\",\"form_of_employment\":\"FULL_TIME_EMPLOYEE\",\"until_now\":true},\"companies\":[{\"id\":\"1_abcdef\",\"name\":\"XING AG\",\"title\":\"Softwareentwickler\",\"company_size\":\"201-500\",\"tag\":null,\"url\":\"http://www.xing.com\",\"career_level\":\"PROFESSIONAL_EXPERIENCED\",\"begin_date\":\"2010-01\",\"description\":null,\"end_date\":null,\"industry\":\"AEROSPACE\",\"form_of_employment\":\"FULL_TIME_EMPLOYEE\",\"until_now\":true},{\"id\":\"24_abcdef\",\"name\":\"Ninja Ltd.\",\"title\":\"DevOps\",\"company_size\":null,\"tag\":\"NINJA\",\"url\":\"http://www.ninja-ltd.co.uk\",\"career_level\":null,\"begin_date\":\"2009-04\",\"description\":null,\"end_date\":\"2010-07\",\"industry\":\"ALTERNATIVE_MEDICINE\",\"form_of_employment\":\"OWNER\",\"until_now\":false},{\"id\":\"45_abcdef\",\"name\":null,\"title\":\"Wiss. Mitarbeiter\",\"company_size\":null,\"tag\":\"OFFIS\",\"url\":\"http://www.uni.de\",\"career_level\":null,\"begin_date\":\"2007\",\"description\":null,\"end_date\":\"2008\",\"industry\":\"APPAREL_AND_FASHION\",\"form_of_employment\":\"PART_TIME_EMPLOYEE\",\"until_now\":false},{\"id\":\"176_abcdef\",\"name\":null,\"title\":\"TEST NINJA\",\"company_size\":\"201-500\",\"tag\":\"TESTCOMPANY\",\"url\":null,\"career_level\":\"ENTRY_LEVEL\",\"begin_date\":\"1998-12\",\"description\":null,\"end_date\":\"1999-05\",\"industry\":\"ARTS_AND_CRAFTS\",\"form_of_employment\":\"INTERN\",\"until_now\":false}],\"awards\":[{\"name\":\"Awesome Dude Of The Year\",\"date_awarded\":2007,\"url\":null}]},\"educational_background\":{\"degree\":\"MSc CE/CS\",\"primary_school\":{\"id\":\"42_abcdef\",\"name\":\"Carl-von-Ossietzky Universtät Schellenburg\",\"degree\":\"MSc CE/CS\",\"notes\":null,\"subject\":null,\"begin_date\":\"1998-08\",\"end_date\":\"2005-02\"},\"schools\":[{\"id\":\"42_abcdef\",\"name\":\"Carl-von-Ossietzky Universtät Schellenburg\",\"degree\":\"MSc CE/CS\",\"notes\":null,\"subject\":null,\"begin_date\":\"1998-08\",\"end_date\":\"2005-02\"}],\"qualifications\":[\"TOEFLS\",\"PADI AOWD\"]},\"photo_urls\":{\"large\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.140x185.jpg\",\"maxi_thumb\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.70x93.jpg\",\"medium_thumb\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.57x75.jpg\",\"mini_thumb\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.18x24.jpg\",\"thumb\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.30x40.jpg\",\"size_32x32\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.32x32.jpg\",\"size_48x48\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.48x48.jpg\",\"size_64x64\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.64x64.jpg\",\"size_96x96\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.96x96.jpg\",\"size_128x128\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.128x128.jpg\",\"size_192x192\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.192x192.jpg\",\"size_256x256\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.256x256.jpg\",\"size_1024x1024\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.1024x1024.jpg\",\"size_original\":\"http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.original.jpg\"}}]}";
        private XingClientDescendant _descendant;

        [SetUp]
        public void SetUp()
        {
            var factory = Substitute.For<IRequestFactory>();
            factory.CreateClient().Returns(Substitute.For<IRestClient>());
            factory.CreateRequest().Returns(Substitute.For<IRestRequest>());

            _descendant = new XingClientDescendant(factory, Substitute.For<IClientConfiguration>());
        }

        [Test]
        public void Should_ReturnCorrectRequestTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetRequestTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.xing.com");
            endpoint.Resource.Should().Be("/v1/request_token");
        }

        [Test]
        public void Should_ReturnCorrectLoginServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetLoginServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.xing.com");
            endpoint.Resource.Should().Be("/v1/authorize");
        }

        [Test]
        public void Should_ReturnCorrectAccessTokenServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetAccessTokenServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.xing.com");
            endpoint.Resource.Should().Be("/v1/access_token");
        }

        [Test]
        public void Should_ReturnCorrectUserInfoServiceEndpoint()
        {
            // act
            var endpoint = _descendant.GetUserInfoServiceEndpoint();

            // assert
            endpoint.BaseUri.Should().Be("https://api.xing.com");
            endpoint.Resource.Should().Be("/v1/users/me");
        }

        [Test]
        public void Should_ParseAllFieldsOfUserInfo_WhenCorrectContentIsPassed()
        {
            // act
            var info = _descendant.ParseUserInfo(Content);

            // assert
            info.Id.Should().Be("123456_abcdef");
            info.FirstName.Should().Be("Max");
            info.LastName.Should().Be("Mustermann");
            info.Email.Should().Be("max.mustermann@xing.com");
            info.PhotoUri.Should().Be("http://www.xing.com/img/users/e/3/d/f94ef165a.123456,1.128x128.jpg");
        }

        private class XingClientDescendant : XingClient
        {
            public XingClientDescendant(IRequestFactory factory, IClientConfiguration configuration)
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
