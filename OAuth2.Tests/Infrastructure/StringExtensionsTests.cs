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
        public void Fill_FormatAndArgs_ReturnsFormattedString(string formatted, string format, params object[] args)
        {
            // arrange & act & assert
            format.Fill(args).Should().Be(formatted);
        }

        [Test]
        [TestCase("one,two,3", ",", "one", "two", 3)]
        public void Join_CollectionAndSeparator_ReturnsJoinedString(string joinResult, string separator, params object[] collection)
        {
            // arrange & act & assert
            collection.Join(separator).Should().Be(joinResult);
        }

        [Test]
        [TestCase(true, null, "String is null.")]
        [TestCase(true, "", "String is empty.")]
        [TestCase(true, "\n", "String contains only new line character.")]
        [TestCase(true, "\t", "String contains only tab character.")]
        [TestCase(true, "  ", "String is composed of spaces.")]
        [TestCase(false, "Not empty.", null)]
        public void IsEmpty_VariousStrings_ReturnsExpectedResult(bool testResult, string @string, string message)
        {
            // arrange & act & assert
            @string.IsEmpty().Should().Be(testResult);
        }

        [Test]
        [TestCase("abba", "54a8723466e5d487247f3d93d51c66bc")]
        public void GetMd5Hash_GivenString_ReturnsExpectedHash(string given, string expected)
        {
            // arrange & act & assert
            given.GetMd5Hash().Should().Be(expected);
        }
    }
}
