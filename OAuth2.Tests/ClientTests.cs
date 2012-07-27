using NUnit.Framework;
using OAuth2.Client;

namespace OAuth2.Tests
{
    [TestFixture]
    public class ClientTests
    {
        [Test]
        public void Test()
        {
            var client = new GoogleClient(null, null, null);
            var uri = client.GetAccessCodeRequestUri();
        }
    }
}