using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using OAuth2.Infrastructure;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class EnumerableExtensionsTests
    {
        [Test]
        public void ForEach_MultipleItems_CallsActionForEachItem()
        {
            // arrange
            var items = new[] { 1, 2, 3, 4, 5 };
            var visitedItems = new List<int>();

            // act
            items.ForEach(visitedItems.Add);

            // assert
            visitedItems.Should().BeEquivalentTo(items);
        }
    }
}
