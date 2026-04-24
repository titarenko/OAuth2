using System;
using RestSharp;

namespace OAuth2.Client
{
    /// <summary>
    /// Indicates unexpected response from service.
    /// </summary>
    public class UnexpectedResponseException : Exception
    {
        /// <summary>
        /// Name of field which contains unexpected (GET) response.
        /// </summary>
        public string? FieldName { get; set; }

        /// <summary>
        /// OAuth error code returned by the provider (e.g. "access_denied", "invalid_request").
        /// Populated when the error originates from an OAuth callback per RFC 6749 §4.1.2.1.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Human-readable error description returned by the provider.
        /// Populated when the error originates from an OAuth callback per RFC 6749 §4.1.2.1.
        /// </summary>
        public string? ErrorDescription { get; set; }

        /// <summary>
        /// Unexpected response itself (can be null, if error occurred later in the response processing pipeline).
        /// </summary>
        public RestResponse? Response { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedResponseException"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public UnexpectedResponseException(RestResponse response)
            : base(BuildMessage(response))
        {
            Response = response;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedResponseException"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        public UnexpectedResponseException(string fieldName)
            : base($"Unexpected value of '{fieldName}' received.")
        {
            FieldName = fieldName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedResponseException"/> class
        /// with OAuth error details from the provider callback.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="errorCode">OAuth error code (e.g. "access_denied").</param>
        /// <param name="errorDescription">Human-readable error description from the provider.</param>
        public UnexpectedResponseException(string fieldName, string? errorCode, string? errorDescription)
            : base(BuildMessage(fieldName, errorCode, errorDescription))
        {
            FieldName = fieldName;
            ErrorCode = errorCode;
            ErrorDescription = errorDescription;
        }

        private static string BuildMessage(RestResponse response)
        {
            return $"Unexpected response: {(int)response.StatusCode} {response.StatusDescription}";
        }

        private static string BuildMessage(string fieldName, string? errorCode, string? errorDescription)
        {
            if (!String.IsNullOrEmpty(errorDescription))
                return $"OAuth error '{errorCode}': {errorDescription}";
            if (!String.IsNullOrEmpty(errorCode))
                return $"OAuth error '{errorCode}' received.";
            return $"Unexpected value of '{fieldName}' received.";
        }
    }
}
