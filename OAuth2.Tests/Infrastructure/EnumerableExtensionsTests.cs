using System.Collections.Generic;
using NUnit.Framework;
using OAuth2.Infrastructure;
using FluentAssertions;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class EnumerableExtensionsTests
    {
        [Test]
        public void Should_CallActionForEachItem_WhenForEachIsInvoked()
        {
            // arrange
            var items = new[] {1, 2, 3, 4, 5};
            var visitedItems = new List<int>();

            // act
            items.ForEach(visitedItems.Add);

            // assert
            visitedItems.Should().BeEquivalentTo(items);
        }
    }
}