using RestSharp.Portable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// REST request extensions
    /// </summary>
    public static class RestRequestExtensions
    {
        /// <summary>
        /// Port of AddObject to RestSharp.Portable
        /// </summary>
        /// <param name="request"></param>
        /// <param name="obj"></param>
        /// <param name="includedProperties"></param>
        /// <returns></returns>
        public static IRestRequest AddObject(this IRestRequest request, object obj, params string[] includedProperties)
        {
            // automatically create parameters from object props
            var type = obj.GetType();
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                bool isAllowed = includedProperties.Length == 0 ||
                                 (includedProperties.Length > 0 && includedProperties.Contains(prop.Name));

                if (isAllowed)
                {
                    var propType = prop.PropertyType;
                    var val = prop.GetValue(obj, null);

                    if (val != null)
                    {
                        if (propType.IsArray)
                        {
                            var elementType = propType.GetElementType();

                            if (((Array)val).Length > 0 &&
                                (elementType.IsPrimitive || elementType.IsValueType || elementType == typeof(string)))
                            {
                                // convert the array to an array of strings
                                var values =
                                    (from object item in ((Array)val) select item.ToString()).ToArray<string>();
                                val = string.Join(",", values);
                            }
                            else
                            {
                                // try to cast it
                                val = string.Join(",", (string[])val);
                            }
                        }

                        request.AddParameter(prop.Name, val);
                    }
                }
            }

            return request;
        }
    }
}
