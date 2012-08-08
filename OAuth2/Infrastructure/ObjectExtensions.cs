using System.Linq;

namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Common extensions.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Returns true if all equally named and typed properties have 
        /// same values on two different objects (types of objects can be different).
        /// </summary>
        public static bool AllPropertiesAreEqualTo(this object @this, object other)
        {
            var thisProperties = @this.GetType().GetProperties().Where(x => x.CanRead).ToList();
            var otherProperties = other.GetType().GetProperties().Where(x => x.CanRead).ToList();

            return (from thisProperty in thisProperties
                    let otherProperty = otherProperties.FirstOrDefault(
                        x => x.Name == thisProperty.Name &&
                             x.PropertyType == thisProperty.PropertyType)
                    where otherProperty != null &&
                          thisProperty.GetValue(@this, null).Equals(otherProperty.GetValue(other, null))
                    select 1).Sum() == thisProperties.Count;
        }
    }
}