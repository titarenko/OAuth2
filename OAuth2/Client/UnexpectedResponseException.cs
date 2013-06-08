using System;
using RestSharp;

namespace OAuth2.Client
{
    public class UnexpectedResponseException : Exception
    {
        public string FieldName { get; set; }

        public IRestResponse Response { get; private set; }

        public UnexpectedResponseException(IRestResponse response)
        {
            Response = response;
        }

        public UnexpectedResponseException(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}