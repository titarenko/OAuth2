using FluentAssertions;
using NUnit.Framework;
using OAuth2.Infrastructure;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [Test]
        [TestCase("key.subkey", "{0}.{1}", "key", "subkey")]
        public void Should_ReturnFormattedString_WhenFillIsCalled(string formatted, string format, params object[] args)
        {
            format.Fill(args).Should().Be(formatted);
        }

        [Test]
        [TestCase("one,two,3", ",", "one", "two", 3)]
        public void Should_ReturnJoinResult_WhenJoinIsCalled(string joinResult, string separator, params object[] collection)
        {
            collection.Join(separator).Should().Be(joinResult);
        }

        [Test]
        [TestCase(true, null, "String is null.")]
        [TestCase(true, "", "String is empty.")]
        [TestCase(true, "\n", "String contains only new line character.")]
        [TestCase(true, "\t", "String contains only tab character.")]
        [TestCase(true, "  ", "String is composed of spaces.")]
        [TestCase(false, "Not empty.", null)]
        public void Should_ReturnTestResult_WhenIsEmptyIsCalledForString(bool testResult, string @string, string message)
        {
            @string.IsEmpty().Should().Be(testResult);
        }

        [Test]
        [TestCase("abba", "54a8723466e5d487247f3d93d51c66bc")]
        public void Should_ReturnExpectedMD5Hash_When_CalledOnGivenString(string given, string expected)
        {
            given.GetMd5Hash().Should().Be(expected);
        }
    }
}