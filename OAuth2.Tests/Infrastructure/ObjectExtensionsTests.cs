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
        public void Should_ReturnTrue_ForObjectsOfAnonymousClass_HavingSameSetsOfProperties_WithSameValues()
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
        public void Should_ReturnTrue_ForObjectsOfAnonymousClass_HavingSameSetsOfProperties_WithSameValues_WhereSomeOfValuesAreNulls()
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
        public void Should_ReturnTrue_ForObjectsOfAnonymousClass_HavingSameSetsOfProperties_WithDifferentValues()
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
        public void Should_ReturnTrue_ForObjectsOfAnonymousClass_HavingSameSetsOfProperties_WithDifferentValues_WhereSomeOfThemAreNotOfReferenceTypes()
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
        public void Should_ReturnTrue_ForObjectsOfAnonymousClass_HavingDifferentSetsOfProperties()
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