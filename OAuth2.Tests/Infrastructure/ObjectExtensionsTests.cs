using System;
using NUnit.Framework;
using OAuth2.Infrastructure;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class ObjectExtensionsTests
    {
        [Test]
        public void AllPropertiesAreEqualTo_SameValues_ReturnsTrue()
        {
            // arrange
            var left = new
            {
                number = 10,
                real = 1.1m,
                text = "text"
            };
            var right = new
            {
                number = 10,
                real = 1.1m,
                text = "text"
            };

            // act & assert
            left.AllPropertiesAreEqualTo(right).Should().BeTrue();
        }

        [Test]
        public void AllPropertiesAreEqualTo_SameValuesWithNulls_ReturnsTrue()
        {
            // arrange
            var left = new
            {
                number = 3,
                uri = new Uri("http://example.com"),
                text = (string) null
            };
            var right = new
            {
                number = 3,
                uri = new Uri("http://example.com"),
                text = (string)null
            };

            // act & assert
            left.AllPropertiesAreEqualTo(right).Should().BeTrue();
        }

        [Test]
        public void AllPropertiesAreEqualTo_DifferentReferenceValues_ReturnsFalse()
        {
            // arrange
            var left = new
            {
                number = 7,
                uri = new Uri("http://demo.example.com")
            };
            var right = new
            {
                number = 7,
                uri = new Uri("http://example.com")
            };

            // act & assert
            left.AllPropertiesAreEqualTo(right).Should().BeFalse();
        }

        [Test]
        public void AllPropertiesAreEqualTo_DifferentValueTypeValues_ReturnsFalse()
        {
            // arrange
            var left = new
            {
                number = 3,
                uri = new Uri("http://example.com")
            };
            var right = new
            {
                number = 7,
                uri = new Uri("http://example.com")
            };

            // act & assert
            left.AllPropertiesAreEqualTo(right).Should().BeFalse();
        }

        [Test]
        public void AllPropertiesAreEqualTo_DifferentPropertySets_ReturnsFalse()
        {
            // arrange
            var left = new
            {
                number = 3,
                uri = new Uri("http://example.com"),
                text = "text"
            };
            var right = new
            {
                number = 7,
                uri = new Uri("http://example.com")
            };

            // act & assert
            left.AllPropertiesAreEqualTo(right).Should().BeFalse();
        }
    }
}
