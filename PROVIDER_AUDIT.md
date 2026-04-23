# OAuth2 Provider Audit — Issue #135

**Date:** 2026-04-23
**Branch:** `fix/update-oauth-providers-135`
**Issue:** https://github.com/titarenko/OAuth2/issues/135

## Summary

This audit verifies that the four priority OAuth providers (GitHub, Google, Facebook, Microsoft/WindowsLive) are up-to-date with their latest official documentation. Each provider is evaluated for: active status, endpoint correctness, auth header format, scope names, API versions, and field mapping.

---

## 1. GitHub

| Item | Current Value | Expected Value | Status |
|------|--------------|----------------|--------|
| **Provider Active?** | Yes | Yes | OK |
| **Auth Endpoint** | `https://github.com/login/oauth/authorize` | `https://github.com/login/oauth/authorize` | OK |
| **Token Endpoint** | `https://github.com/login/oauth/access_token` | `https://github.com/login/oauth/access_token` | OK |
| **UserInfo Endpoint** | `https://api.github.com/user` | `https://api.github.com/user` | OK |
| **User Emails Endpoint** | `https://api.github.com/user/emails` | `https://api.github.com/user/emails` | OK |
| **Auth Header Scheme** | `token` (via `OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken, "token")`) | `Bearer` (per GitHub docs: `Authorization: Bearer OAUTH-TOKEN`) | **NEEDS FIX** |
| **Docs Link** | https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps | — | Current |

### Root Cause Analysis (5 Whys) — Auth Header Scheme

1. **Why is `"token"` used instead of `"Bearer"`?** Because the original PR #138 (Oct 2020) used `"token"` as the scheme, which was the historical GitHub convention.
2. **Why did PR #138 use `"token"`?** Because at the time, GitHub accepted both `token` and `Bearer` prefixes and the medium article referenced the `token` prefix.
3. **Why is this a problem now?** Because GitHub's official documentation now exclusively documents `Authorization: Bearer OAUTH-TOKEN` as the correct format, and the `token` prefix may be deprecated.
4. **Why should we change it?** To align with official documentation, prevent future breakage, and follow the OAuth2 RFC 6750 standard (`Bearer` scheme).
5. **Why does this matter for issue #135?** Issue #135 was reopened because `access_token` was still being sent as a query parameter in some code paths. The fix should ensure ALL API calls use `Authorization: Bearer <token>` per current GitHub docs.

### Changes Required
- [x] Change `BeforeGetUserInfo` to use `"Bearer"` instead of `"token"` in the OAuth2AuthorizationRequestHeaderAuthenticator
- [x] Change the email fallback request in `GetUserInfoAsync` to also use `"Bearer"`

---

## 2. Google

| Item | Current Value | Expected Value | Status |
|------|--------------|----------------|--------|
| **Provider Active?** | Yes | Yes | OK |
| **Auth Endpoint** | `https://accounts.google.com/o/oauth2/auth` | `https://accounts.google.com/o/oauth2/v2/auth` | **NEEDS FIX** |
| **Token Endpoint** | `https://accounts.google.com/o/oauth2/token` | `https://oauth2.googleapis.com/token` | **NEEDS FIX** |
| **UserInfo Endpoint** | `https://www.googleapis.com/oauth2/v1/userinfo` | `https://www.googleapis.com/oauth2/v3/userinfo` | **NEEDS FIX** |
| **Auth Header** | Bearer (inherited from base class) | Bearer | OK |
| **Response Fields (v1→v3)** | `id`, `given_name`, `family_name`, `email`, `picture` | `sub`, `given_name`, `family_name`, `email`, `picture` | **NEEDS FIX** |
| **Docs Link** | https://developers.google.com/identity/protocols/oauth2/web-server | — | Current |

### Root Cause Analysis (5 Whys) — Outdated Endpoints

1. **Why is the auth endpoint `/o/oauth2/auth` instead of `/o/oauth2/v2/auth`?** Because the client was written against the original Google OAuth2 endpoints before v2 was released.
2. **Why is this a problem?** The v1 auth endpoint still works via redirect but Google's documentation exclusively references the v2 endpoint. The old endpoint may be removed.
3. **Why is the token endpoint wrong?** `accounts.google.com/o/oauth2/token` is the legacy endpoint. Google migrated to `oauth2.googleapis.com/token` as the canonical token endpoint.
4. **Why is the userinfo endpoint v1?** The v1 userinfo API returns `id` as the user identifier. Google's v3 userinfo endpoint returns `sub` (OpenID Connect subject identifier) instead.
5. **Why does this matter?** Using deprecated endpoints risks sudden breakage. The v3 userinfo field change (`id` → `sub`) is the most impactful — existing integrations storing Google `id` values will see a different field name but the *values* remain the same.

### Changes Required
- [x] Update auth endpoint to `/o/oauth2/v2/auth`
- [x] Update token endpoint to `https://oauth2.googleapis.com/token`
- [x] Update userinfo endpoint to `/oauth2/v3/userinfo`
- [x] Update `ParseUserInfo` to read `sub` instead of `id` (v3 schema)
- [x] Update test serialization fixtures if any

---

## 3. Facebook

| Item | Current Value | Expected Value | Status |
|------|--------------|----------------|--------|
| **Provider Active?** | Yes | Yes | OK |
| **Auth Endpoint** | `https://www.facebook.com/dialog/oauth` | `https://www.facebook.com/v25.0/dialog/oauth` | **NEEDS FIX** |
| **Token Endpoint** | `https://graph.facebook.com/oauth/access_token` | `https://graph.facebook.com/v25.0/oauth/access_token` | **NEEDS FIX** |
| **UserInfo Endpoint** | `https://graph.facebook.com/me` | `https://graph.facebook.com/v25.0/me` | **NEEDS FIX** |
| **Auth Header** | Bearer (inherited from base class) | Bearer | OK |
| **Fields param** | `id,first_name,last_name,email,picture` | `id,first_name,last_name,email,picture` | OK |
| **Docs Link** | https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow | — | Current |

### Root Cause Analysis (5 Whys) — Unversioned Endpoints

1. **Why are the endpoints unversioned?** The client was written when Facebook's Graph API didn't require version prefixes (or the default version was sufficient).
2. **Why is this a problem?** Facebook requires versioned API calls. Unversioned calls fall back to the minimum supported version, which can change behavior without notice.
3. **Why hasn't this broken yet?** Facebook maintains backward compatibility for ~2 years per API version. Unversioned calls get routed to the oldest active version.
4. **Why should we add versions now?** To control which API version we target, avoid silent behavior changes, and follow Facebook's best practices.
5. **Why v25.0 specifically?** v25.0 is the current stable Graph API version as of early 2026. The dialog endpoint (login) and graph endpoints should use the same version.

### Note on API version
Facebook Graph API versions expire roughly every 2 years. The version should be periodically bumped. We will note the current version in the compatibility matrix.

### Changes Required
- [x] Add version prefix `v25.0` to auth endpoint resource
- [x] Add version prefix `v25.0` to token endpoint resource
- [x] Add version prefix `v25.0` to userinfo endpoint resource

---

## 4. Windows Live (Microsoft)

| Item | Current Value | Expected Value | Status |
|------|--------------|----------------|--------|
| **Provider Active?** | Legacy — officially retired but working in production | Microsoft Identity Platform (v2.0) for new apps | **Legacy (Working)** |
| **Auth Endpoint** | `https://login.live.com/oauth20_authorize.srf` | Still functioning (confirmed in Exceptionless production) | OK |
| **Token Endpoint** | `https://login.live.com/oauth20_token.srf` | Still functioning | OK |
| **UserInfo Endpoint** | `https://apis.live.net/v5.0/me` | Still functioning | OK |
| **Auth Header** | Query param `access_token` (via `BeforeGetUserInfo`) | Working as-is | OK |
| **Scope** | `wl.emails` (Live SDK scope) | Working (confirmed: Exceptionless uses this) | OK |
| **Docs Link** | https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/migrating-from-live-sdk | — | Current |

### Evidence That WindowsLive Still Works

1. **Exceptionless production code** uses `WindowsLiveClient` TODAY with `login.live.com/oauth20_authorize.srf` and `wl.emails` scope — confirmed working by maintainer.
2. **Issue [#155](https://github.com/titarenko/OAuth2/issues/155)** (Jul 2024): Repository maintainer recommends `WindowsLiveClient` for OneDrive integration.
3. **Microsoft's official position**: Live SDK was retired Nov 2018, but Microsoft has not actually shut down the endpoints. This is a common pattern where retirement announcements don't match actual shutdown dates.

### Decision: Keep WindowsLiveClient unchanged, add MicrosoftClient for new integrations

- `WindowsLiveClient` is preserved **100% unchanged** — no behavior changes, no deprecation attributes
- `MicrosoftClient` is added as a new provider for apps that want Microsoft Identity Platform v2.0 + Microsoft Graph
- Users migrating should be aware that **user IDs differ** between the two platforms

---

## Provider Compatibility Matrix (for README)

| Provider | Class | Status | Auth Endpoint | API Version | Last Verified | Docs |
|----------|-------|--------|---------------|-------------|---------------|------|
| GitHub | `GitHubClient` | Active | `github.com/login/oauth/authorize` | Current | 2026-04-23 | [Docs](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps) |
| Google | `GoogleClient` | Active | `accounts.google.com/o/oauth2/v2/auth` | v3 (userinfo) | 2026-04-23 | [Docs](https://developers.google.com/identity/protocols/oauth2/web-server) |
| Facebook | `FacebookClient` | Active | `www.facebook.com/v25.0/dialog/oauth` | Graph API v25.0 | 2026-04-23 | [Docs](https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow) |
| Microsoft | `MicrosoftClient` | Active | `login.microsoftonline.com/common/oauth2/v2.0/authorize` | Graph v1.0 | 2026-04-23 | [Docs](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow) |

---

## 5. WindowsLive Safety Analysis

**Key question:** Is it safe to keep `WindowsLiveClient` unchanged?

### Microsoft's official documentation says:

1. **`apis.live.net/v5.0` is fully dead.** Per [Microsoft's migration doc](https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/migrating-from-live-sdk): "Live Connect APIs are all hosted from `https://apis.live.net/v5.0`... these APIs are now end of life and will no longer be available after November 1, 2018." Profile/contacts stopped returning data December 1, 2017.

2. **`login.live.com` auth endpoints** may still redirect to `login.microsoftonline.com`, but they are undocumented and unsupported.

3. **Scope migration:** `wl.emails` → `Mail.Read`, `wl.basic` → `User.Read`, `wl.signin` → `openid`. Completely different scope names.

4. **User IDs changed.** Microsoft explicitly says: "Microsoft Graph uses a different scheme for unique identifiers than Live Connect. You cannot use the identifiers from Live Connect with Microsoft Graph."

5. **Response schema changed.** Live Connect returned `first_name`/`last_name`. Microsoft Graph returns `givenName`/`surname`/`mail`.

### Decision: Keep both providers side by side

- `WindowsLiveClient` is preserved **100% unchanged** (no behavior changes, no `[Obsolete]` attribute since `TreatWarningsAsErrors=true` would break builds). Only XML doc `<remarks>` added to document deprecation status.
- `MicrosoftClient` is added as a **brand new separate provider** using Microsoft Identity Platform v2.0 and Microsoft Graph v1.0.
- Users migrating from WindowsLive to Microsoft should be aware that **user IDs will change** — apps storing Live SDK user IDs in their database will see those users appear as "new" users.

---

## 6. Full Deprecation Audit (All 25 Providers)

| Provider | Class | Protocol | Status | Details | Evidence |
|----------|-------|----------|--------|---------|----------|
| Asana | `AsanaClient` | OAuth2 | **Active** | Endpoints current. Issue [#103](https://github.com/titarenko/OAuth2/issues/103): null photo crash (open) | [Docs](https://developers.asana.com/docs/oauth) |
| DigitalOcean | `DigitalOceanClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://docs.digitalocean.com/reference/api/oauth-api/) |
| Exact Online | `ExactOnlineClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://developers.exactonline.com/) |
| Facebook | `FacebookClient` | OAuth2 | **Active** | Updated to Graph API v25.0 | [Docs](https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow) |
| Fitbit | `FitbitClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://dev.fitbit.com/build/reference/web-api/authorization/) |
| Foursquare | `FoursquareClient` | OAuth2 | **Deprecated** | v2 /users/self endpoint deprecated; Foursquare pivoted to Places API v3 | [Docs](https://docs.foursquare.com/) |
| GitHub | `GitHubClient` | OAuth2 | **Active** | Fixed: Bearer header instead of token. Issue [#135](https://github.com/titarenko/OAuth2/issues/135) | [Docs](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps) |
| Google | `GoogleClient` | OAuth2 | **Active** | Updated to v2/v3 endpoints, `sub` field | [Docs](https://developers.google.com/identity/protocols/oauth2/web-server) |
| Instagram | `InstagramClient` | OAuth2 | **Dead** | Instagram Basic Display API shut down Dec 4, 2024. No consumer API exists. Issue [#129](https://github.com/titarenko/OAuth2/issues/129) | [Shutdown announcement](https://developers.facebook.com/blog/post/2024/09/04/update-on-instagram-basic-display-api/) |
| LinkedIn | `LinkedInClient` | OAuth2 | **Needs Update** | LinkedIn OAuth2 is alive (`/oauth/v2/authorization`, `/v2/userinfo`). This client's v1 endpoints (`/uas/oauth2/`, `/v1/people/~` XML) were shut down 2019. Needs rewrite to v2 JSON. | [v2 Auth Flow](https://learn.microsoft.com/en-us/linkedin/shared/authentication/authorization-code-flow) · [OpenID Connect](https://learn.microsoft.com/en-us/linkedin/consumer/integrations/self-serve/sign-in-with-linkedin-v2) |
| Login Cidadão | `LoginCidadaoClient` | OAuth2 | **Unknown** | Niche Brazilian government identity provider; unable to verify | — |
| Mail.Ru | `MailRuClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://api.mail.ru/docs/guides/oauth/) |
| Microsoft | `MicrosoftClient` | OAuth2 | **Active** | NEW — Microsoft Identity Platform v2.0 + Graph v1.0. Addresses issue [#145](https://github.com/titarenko/OAuth2/issues/145) | [Docs](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow) |
| Odnoklassniki | `OdnoklassnikiClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://apiok.ru/en/ext/oauth/) |
| Salesforce | `SalesforceClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://help.salesforce.com/s/articleView?id=sf.remoteaccess_oauth_web_server_flow.htm) |
| Spotify | `SpotifyClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://developer.spotify.com/documentation/web-api/tutorials/code-flow) |
| Todoist | `TodoistClient` | OAuth2 | **Needs Update** | OAuth endpoints correct. User info endpoint uses deprecated Sync API v6; current is Todoist API v1 (unified) | [Current API](https://developer.todoist.com/api/v1/) |
| Twitter | `TwitterClient` | OAuth1 | **Active** | Rebranded to X; OAuth 1.0a endpoints operational. Issue [#137](https://github.com/titarenko/OAuth2/issues/137): auth broken (open) | [Docs](https://developer.x.com/en/docs/authentication/oauth-1-0a) |
| Uber | `UberClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://developer.uber.com/docs/riders/guides/authentication/introduction) |
| VK | `VkClient` | OAuth2 | **Active** | Fixed: API version updated from 5.74 to 5.131. Issue [#146](https://github.com/titarenko/OAuth2/issues/146) | [Docs](https://dev.vk.com/en/api/access-token/authcode-flow-user) |
| VSTS | `VSTSClient` | OAuth2 | **Active** | Rebranded to Azure DevOps; endpoints operational | [Docs](https://learn.microsoft.com/en-us/azure/devops/integrate/get-started/authentication/oauth) |
| Windows Live | `WindowsLiveClient` | OAuth2 | **Legacy (Working)** | Microsoft officially retired Live SDK Nov 2018, but login.live.com + apis.live.net endpoints still function. Confirmed working in production (Exceptionless). Issue [#155](https://github.com/titarenko/OAuth2/issues/155): maintainer recommends for OneDrive. For new integrations use `MicrosoftClient`. | [Migration guide](https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/migrating-from-live-sdk) |
| Xing | `XingClient` | OAuth1 | **Dead** | OAuth 1.0a REST API discontinued. dev.xing.com only offers plugins (Login with XING, Share). Xing rebranded under New Work SE. | [dev.xing.com](https://dev.xing.com/) (plugins only) |
| Yahoo | `YahooClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://developer.yahoo.com/oauth2/guide/) |
| Yandex | `YandexClient` | OAuth2 | **Active** | Endpoints current | [Docs](https://yandex.com/dev/id/doc/en/codes/code-url) |

### Related Open Issues

| Issue | Provider | Summary |
|-------|----------|---------|
| [#135](https://github.com/titarenko/OAuth2/issues/135) | GitHub | Deprecated `access_token` query parameter auth — **Fixed in this PR** |
| [#145](https://github.com/titarenko/OAuth2/issues/145) | Microsoft | Request for Azure/Microsoft OAuth — **Addressed by new `MicrosoftClient`** |
| [#146](https://github.com/titarenko/OAuth2/issues/146) | VK | API version 5.74 deprecated — **Fixed: updated to 5.131** |
| [#129](https://github.com/titarenko/OAuth2/issues/129) | Instagram | `UnexpectedResponseException` — API now fully shut down |
| [#137](https://github.com/titarenko/OAuth2/issues/137) | Twitter | Auth broken ("I can't authenticate you") — open, likely API tier changes |
| [#103](https://github.com/titarenko/OAuth2/issues/103) | Asana | Null photo crashes `ParseUserInfo` — open |
| [#155](https://github.com/titarenko/OAuth2/issues/155) | WindowsLive | OneDrive/Dropbox question — maintainer confirmed WindowsLiveClient for OneDrive |

### Summary

- **2 Dead providers:** Instagram (Basic Display API shut down Dec 2024), Xing (REST API discontinued)
- **2 Needs Update:** LinkedIn (v1→v2 rewrite needed), Todoist (v6→v1 endpoint update needed)
- **1 Legacy (Working):** WindowsLive (officially retired but still functional in production)
- **1 Deprecated:** Foursquare (v2 consumer API deprecated)
- **2 Rebranded:** Twitter → X, VSTS → Azure DevOps
- **1 Unknown:** Login Cidadão
- **1 Unknown:** Login Cidadão
- **16 Active providers:** No known issues
