using RestSharp.Portable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OAuth2.Infrastructure
{
    public static class RestResponseExtensions
    {
        private static System.Text.Encoding _defaultContentEncoding = System.Text.Encoding.UTF8;

        /// <summary>
        /// IsEmpty for RestSharp.Portable
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool IsEmpty(this IRestResponse response)
        {
            var data = response.RawBytes;
            if (data == null)
                return true;
            if (data.All(x => x == 0 || x == 9 || x == 32))
                return true;
            return false;
        }

        /// <summary>
        /// Replacement of "Content" property for RestSharp.Portable
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string GetContent(this IRestResponse response)
        {
            return response.GetContent(_defaultContentEncoding);
        }

        /// <summary>
        /// Replacement of "Content" property for RestSharp.Portable
        /// </summary>
        /// <param name="response"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetContent(this IRestResponse response, System.Text.Encoding encoding)
        {
            var data = response.RawBytes;
            if (data == null)
                return null;
            return encoding.GetString(data, 0, data.Length);
        }
    }
}
