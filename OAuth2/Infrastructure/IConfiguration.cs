using System;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Defines configuration API.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Returns configuration section by name.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <param name="allowInheritance">Allows read values from parent section if true.</param>
        IConfiguration GetSection(string name, bool allowInheritance = true);

        /// <summary>
        /// Returns configuration section for given type (uses type name as section name).
        /// </summary>
        /// <param name="allowInheritance">Allows read values from parent section if true.</param>
        IConfiguration GetSection<T>(bool allowInheritance = true);

        /// <summary>
        /// Returns configuration section for given type (uses type name as section name).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="allowInheritance">Allows read values from parent section if true.</param>
        IConfiguration GetSection(Type type, bool allowInheritance = true);

        /// <summary>
        /// Returns value by key.
        /// </summary>
        string Get(string key);

        /// <summary>
        /// Returns instance with properties initialized from configuration values.
        /// </summary>
        T Get<T>();

        /// <summary>
        /// Returns strongly typed value by key.
        /// </summary>
        T Get<T>(string key);
    }
}