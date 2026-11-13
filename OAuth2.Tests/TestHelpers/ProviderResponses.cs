namespace OAuth2.Tests.TestHelpers
{
    // Published examples and sanitized upstream fixtures, not authenticated captures from this repository.
    // Tests explicitly mutate these baselines for optional fields and invalid-input cases.
    internal static class ProviderResponses
    {
        // /oauth2/v3/userinfo fixture; name and email replaced by fictional example values:
        // https://github.com/jaredhanson/passport-google-oauth2/blob/79f9ed6878f1d11150243a1429acb7ba3211e239/test/fixtures/userinfo/userinfo-with-profile-email.json
        // Claim types: https://developers.google.com/identity/openid-connect/reference#userinfo
        /* lang=json */
        public const string GoogleUserInfo = """
            {
              "sub": "111111111111111111111",
              "name": "Example User",
              "given_name": "Example",
              "family_name": "User",
              "picture": "https://lh3.googleusercontent.com/-XxXXxxXxXXX/AAAAAAAAAAI/AAAAAAAAAAA/0000xxxxx0X/photo.jpg",
              "email": "user@example.com",
              "email_verified": true,
              "locale": "en"
            }
            """;

        // Published /v2/userinfo example; email replaced by a reserved example address:
        // https://learn.microsoft.com/en-us/linkedin/consumer/integrations/self-serve/sign-in-with-linkedin-v2#sample-api-response
        /* lang=json */
        public const string LinkedInUserInfo = """
            {
              "sub": "782bbtaQ",
              "name": "John Doe",
              "given_name": "John",
              "family_name": "Doe",
              "picture": "https://media.licdn-ei.com/dms/image/C5F03AQHqK8v7tB1HCQ/profile-displayphoto-shrink_100_100/0/",
              "locale": "en-US",
              "email": "doe@example.com",
              "email_verified": true
            }
            """;

        // Selected fields from Salesforce's sanitized identity-response fixture; unrelated fields omitted.
        // https://github.com/forcedotcom/SalesforceMobileSDK-iOS/blob/c7d68decca33d5b02aeb0ab79082222a623973d5/libs/SalesforceSDKCore/SalesforceSDKCoreTests/SalesforceSDKIdentityTests.m
        // Contract: https://developer.salesforce.com/docs/platform/mobile-sdk/guide/oauth-using-identity-urls.html
        /* lang=json */
        public const string SalesforceIdentity = """
            {
              "id": "https://login.salesforce.com/id/some-org-id/some-user-id",
              "asserted_user": true,
              "user_id": "some-user-id",
              "organization_id": "some-org-id",
              "username": "user@example.com",
              "nick_name": "nickname",
              "display_name": "Example User",
              "email": "user@example.com",
              "email_verified": true,
              "first_name": "Example",
              "last_name": "User",
              "photos": {
                "picture": "https://example.com/profilephoto/full",
                "thumbnail": "https://example.com/profilephoto/thumb"
              },
              "mobile_phone": null,
              "mobile_phone_verified": false
            }
            """;

        // Selected fields from GitHub's GET /user example; email redacted to example.com, unrelated fields omitted.
        // https://docs.github.com/en/rest/users/users#get-the-authenticated-user
        /* lang=json */
        public const string GitHubUser = """
            {
              "login": "octocat",
              "id": 1,
              "avatar_url": "https://github.com/images/error/octocat_happy.gif",
              "name": "monalisa octocat",
              "email": "octocat@example.com"
            }
            """;

        // Published GET /user/emails example; email redacted to example.com:
        // https://docs.github.com/en/rest/users/emails#list-email-addresses-for-the-authenticated-user
        /* lang=json */
        public const string GitHubEmails = """
            [
              {
                "email": "octocat@example.com",
                "verified": true,
                "primary": true,
                "visibility": "public"
              }
            ]
            """;
    }
}
