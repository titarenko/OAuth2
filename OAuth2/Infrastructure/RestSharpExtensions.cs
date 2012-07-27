using System.Linq;
using RestSharp;

namespace OAuth2.Infrastructure
{
    public static class RestSharpExtensions
    {
         public static void AddObjectPropertiesAsParameters(this IRestRequest request, object instance)
         {
             instance.GetType().GetProperties().Where(x => x.CanRead)
                 .ForEach(info => request.AddParameter(info.Name.FromCamelToSnakeCase(), info.GetValue(instance, null)));
         }
    }
}