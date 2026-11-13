using System.Collections.Generic;

namespace OAuth2.Models
{
    /// <summary>
    /// Contains URIs for different sizes of a user's avatar image.
    /// </summary>
    public class AvatarInfo
    {
        /// <summary>
        /// Image size constants.
        /// </summary>
        internal const int SmallSize = 36;
        internal const int LargeSize = 300;

        /// <summary>
        /// Uri of small photo.
        /// </summary>
        public string? Small { get; set; }

        /// <summary>
        /// Uri of normal photo.
        /// </summary>
        public string? Normal { get; set; }

        /// <summary>
        /// Uri of large photo.
        /// </summary>
        public string? Large { get; set; }
    }

    /// <summary>
    /// Contains information about user who is being authenticated.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UserInfo()
        {
            AvatarUri = new AvatarInfo();
        }

        /// <summary>
        /// Identifier assigned by the provider.
        /// </summary>
        /// <remarks>
        /// Use this identifier together with <see cref="ProviderName"/> to look up an already linked account.
        /// Google maps its <c>sub</c> claim here; that value remains unchanged when the user's email address changes.
        /// </remarks>
        /// <seealso href="https://developers.google.com/identity/openid-connect/reference#userinfo">Google userinfo claims</seealso>
        public string? Id { get; set; }

        /// <summary>
        /// Friendly name of <see cref="UserInfo"/> provider (which is, in its turn, the client of OAuth/OAuth2 provider).
        /// </summary>
        /// <remarks>
        /// Supposed to be unique per OAuth/OAuth2 client.
        /// </remarks>
        public string? ProviderName { get; set; }

        /// <summary>
        /// Email address reported by the provider.
        /// </summary>
        /// <remarks>
        /// Users or administrators may change this address, and a mailbox can later be reassigned to another person.
        /// Successful OAuth authentication does not guarantee email verification; providers may return editable or
        /// unverified profile addresses. Check <see cref="EmailVerified"/> and the provider's email-authority rules
        /// before using the address for account linking or recovery. Use <see cref="Id"/> for persistent account lookup.
        /// </remarks>
        public string? Email { get; set; }

        /// <summary>
        /// Whether the provider reports the email address as verified.
        /// </summary>
        /// <remarks>
        /// <c>true</c> means verified, <c>false</c> means unverified, and <c>null</c> means unknown or not mapped by the client.
        /// Treat only <c>is true</c> as an affirmative assertion. Verification methods and email authority depend on the provider.
        /// For Google accounts with a non-Gmail address and no <c>hd</c> claim, even <c>true</c> can reflect historical
        /// verification after mailbox ownership has changed. This value alone does not authorize linking to an existing local account.
        /// </remarks>
        /// <seealso href="https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims">OpenID Connect standard claims</seealso>
        /// <seealso href="https://developers.google.com/identity/gsi/web/guides/verify-google-id-token">Google email-authority guidance</seealso>
        public bool? EmailVerified { get; set; }

        /// <summary>
        /// Selected provider-specific string values, or null when none are supplied.
        /// </summary>
        /// <remarks>
        /// Keys and their meanings are scoped to <see cref="ProviderName"/>. This is not a copy of the complete provider response.
        /// For Google, <c>hd</c> contains the hosted-domain claim unchanged. To restrict access to a Google Workspace domain,
        /// check the provider and compare this claim with the application's expected domain; the email suffix does not prove membership.
        /// The presence of <c>hd</c> alone does not establish email verification or authorize account linking.
        /// </remarks>
        /// <seealso href="https://developers.google.com/identity/openid-connect/reference#userinfo">Google userinfo claims</seealso>
        public Dictionary<string, string>? ProviderData { get; set; }

        /// <summary>
        /// First name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Last name.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Photo URI.
        /// </summary>
        public string? PhotoUri
        {
            get { return AvatarUri.Normal; }
        }

        /// <summary>
        /// Contains URIs of different sizes of avatar.
        /// </summary>
        public AvatarInfo AvatarUri { get; private set; }
    }
}

