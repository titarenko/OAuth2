using System;
using System.Web;

namespace OAuth2.Infrastructure
{
    public static class UriUtility
    {
        /// <summary>
        /// Converts relative URI (e.g. "~/Controller/Action") to absolute one (e.g. "https://example.com/app/Controller/Action").
        /// If URI is already absolute (doesn't start with "~"), then no conversion will be performed.
        /// </summary>
        public static string ToAbsolute(string uri)
        {
            if (!uri.StartsWith("~"))
            {
                return uri;
            }

            if (HttpContext.Current == null)
            {
                throw new ApplicationException(
                    "Cannot resolve absolute URI outside of ASP.NET application " +
                    "(current HTTP context is null).");
            }

            uri = uri.Substring(1); // without "~"

            var request = HttpContext.Current.Request;
            return string.Format("{0}://{1}{2}{3}", 
                request.Url.Scheme,
                request.Url.Authority, 
                request.ApplicationPath == "/" ? null : request.ApplicationPath, 
                uri);
        }
    }
}