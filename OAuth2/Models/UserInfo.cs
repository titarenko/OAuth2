namespace OAuth2.Models
{
    /// <summary>
    /// Contains information about user who is being authenticated.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// First name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Photo URI.
        /// </summary>
        public string PhotoUri { get; set; }
    }
}