using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using OAuth2.Client;
using OAuth2.Infrastructure;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class SafeExtensionsTests
    {
        [Test]
        public async Task SafeGet_NullClient_DoesNotThrow()
        {
            // act & assert
            await ((IClient)null).Awaiting(x => x.SafeGetAsync(z => z.GetLoginLinkUriAsync()))
                .Should().NotThrowAsync<NullReferenceException>();
            (await ((IClient)null).SafeGetAsync(x => x.GetLoginLinkUriAsync())).Should().Be(null);
        }

        [Test]
        public void SafeGet_ValidInstance_ReturnsSelectorResult()
        {
            // arrange
            const string value = "abc";
            string Selector(string x) => x.Substring(1);

            // act & assert
            value.SafeGet(Selector).Should().Be("bc");
        }
    }
}
