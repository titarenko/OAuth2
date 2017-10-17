using System;
using System.Runtime.Serialization;

namespace RestSharpInternal.Authenticators.OAuth
{
#if !SILVERLIGHT && !WINDOWS_PHONE && !WINDOWS_UWP
    [Serializable]
#endif
#if WINDOWS_UWP
    [DataContract]
#endif
    public enum OAuthParameterHandling
    {
        HttpAuthorizationHeader,
        UrlOrPostParameters
    }
}
