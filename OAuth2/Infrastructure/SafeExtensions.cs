using System;

namespace OAuth2.Infrastructure
{
    public static class SafeExtensions
    {
        public static T SafeGet<TModel, T>(this TModel instance, Func<TModel, T> selector)
        {
            return instance == null ? default(T) : selector(instance);
        }
    }
}