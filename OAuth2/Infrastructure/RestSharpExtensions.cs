using System.Linq;
using RestSharp;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Set of extension methods for different types of RestSharp library.
    /// </summary>
    public static class RestSharpExtensions
    {
        /// <summary>
        /// Adds properties of given object as request parameters.
        /// Note: converts names from camel to snake case.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="instance">The instance.</param>
        public static void AddObjectPropertiesAsParameters(this IRestRequest request, object instance)
        {
            instance.GetType().GetProperties().Where(x => x.CanRead)
                .ForEach(info => request.AddParameter(info.Name.FromCamelToSnakeCase(), info.GetValue(instance, null)));
        }
    }
}