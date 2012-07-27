using System;
using System.Configuration;
using System.Linq;

namespace OAuth2
{
    public class Configuration : IConfiguration
    {
        public T Get<T>()
        {
            var instance = Activator.CreateInstance<T>();
            typeof (T).GetProperties()
                .Where(x => x.CanWrite)
                .ForEach(x => x.SetValue(instance, ConfigurationManager.AppSettings[x.Name], null));
            return instance;
        }
    }
}