using System;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Set of extension methods for safe operation on nullable types.
    /// </summary>
    public static class SafeExtensions
    {
        /// <summary>
        /// Executes selector on instance and returns result or returns default value of target type if given instance is null.
        /// </summary>
        public static T SafeGet<TModel, T>(this TModel instance, Func<TModel, T> selector) where TModel : class
        {
            return instance == null ? default(T) : selector(instance);
        }
    }
}