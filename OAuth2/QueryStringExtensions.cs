using System.Linq;

namespace OAuth2
{
    public static class QueryStringExtensions
    {
        public static string ToQuerySrting(this object instance)
        {
            return instance.GetType().GetProperties().Where(x => x.CanRead)
                .Select(x => new
                {
                    Name = x.Name.FromCamelToSnakeCase(),
                    Value = x.GetValue(instance, null)
                })
                .Where(x => x.Value != null)
                .Select(x => "{0}={1}".Fill(x.Name, x.Value))
                .Join("&");
        }
    }
}