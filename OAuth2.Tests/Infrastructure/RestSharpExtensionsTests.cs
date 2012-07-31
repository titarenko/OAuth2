﻿using NSubstitute;
using NUnit.Framework;
using RestSharp;
using OAuth2.Infrastructure;

namespace OAuth2.Tests.Infrastructure
{
    [TestFixture]
    public class RestSharpExtensionsTests
    {
        [Test]
        public void Should_AddObjectPropertiesAsSnakeCaseNamedParameters()
        {
            // arrange
            var instance = new
            {
                ClientId = 15,
                State = "done"
            };

            // act
            var request = Substitute.For<IRestRequest>();

            request.AddObjectPropertiesAsParameters(instance);

            // assert
            request.Received(1).AddParameter(Arg.Is("client_id"), Arg.Is(15));
            request.Received(1).AddParameter(Arg.Is("state"), Arg.Is("done"));
        }
    }
}