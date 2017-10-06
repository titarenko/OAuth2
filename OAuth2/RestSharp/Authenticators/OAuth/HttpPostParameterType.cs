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
    internal enum HttpPostParameterType
    {
        Field,
        File
    }
}
