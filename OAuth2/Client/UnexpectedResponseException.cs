using System;
using RestSharp;

namespace OAuth2.Client
{
    public class UnexpectedResponseException : Exception
    {
        public IRestResponse Response { get; private set; }

        public UnexpectedResponseException(IRestResponse response)
        {
            Response = response;
        }
    }
}