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
| **Provider Active?** | DEPRECATED (Live SDK retired Nov 2018) | Microsoft Identity Platform (v2.0) | **NEEDS FIX** |
| **Auth Endpoint** | `https://login.live.com/oauth20_authorize.srf` | `https://login.microsoftonline.com/common/oauth2/v2.0/authorize` | **NEEDS FIX** |
| **Token Endpoint** | `https://login.live.com/oauth20_token.srf` | `https://login.microsoftonline.com/common/oauth2/v2.0/token` | **NEEDS FIX** |
| **UserInfo Endpoint** | `https://apis.live.net/v5.0/me` | `https://graph.microsoft.com/v1.0/me` | **NEEDS FIX** |
| **Auth Header** | Query param `access_token` (via `BeforeGetUserInfo`) | `Authorization: Bearer <token>` (inherited from base class) | **NEEDS FIX** |
| **Avatar URI** | `cid-{id}.users.storage.live.com` template | Microsoft Graph photo endpoint (or omit) | **NEEDS FIX** |
| **Scope** | `wl.emails` (Live SDK scope) | `User.Read` (Microsoft Graph scope) | **NEEDS FIX** |
| **Response Fields** | `id`, `first_name`, `last_name`, `emails.preferred` | `id`, `givenName`, `surname`, `mail` or `userPrincipalName` | **NEEDS FIX** |
| **Docs Link** | https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow | — | Current |

### Root Cause Analysis (5 Whys) — Entirely Deprecated Platform

1. **Why is `login.live.com` / `apis.live.net` used?** The client was written against the original Windows Live SDK (Live Connect API).
2. **Why is this a problem?** Microsoft retired the Live SDK/Live Connect API in November 2018. The `apis.live.net/v5.0` endpoint may return errors or stop working at any time.
3. **Why hasn't it been updated?** No one submitted a PR to migrate to Microsoft Identity Platform (v2.0) / Microsoft Graph.
4. **Why must it change?** The replacement is Microsoft Identity Platform with Microsoft Graph API. All new and existing apps must use `login.microsoftonline.com` and `graph.microsoft.com`.
5. **Why is this the most critical change?** Every endpoint, every scope, every response field, and the auth mechanism are all different. This is a full rewrite of the client, not just URL updates.

### Changes Required
- [x] Rename class from `WindowsLiveClient` to `MicrosoftClient` (keep `WindowsLiveClient` as deprecated alias)
- [x] Update auth endpoint to Microsoft Identity Platform v2.0
- [x] Update token endpoint to Microsoft Identity Platform v2.0
- [x] Update userinfo endpoint to Microsoft Graph v1.0
- [x] Remove `BeforeGetUserInfo` override (base class uses Authorization: Bearer header correctly)
- [x] Rewrite `ParseUserInfo` for Microsoft Graph response schema (`givenName`, `surname`, `mail`)
- [x] Update avatar handling (Microsoft Graph photo endpoint or graceful absence)
- [x] Update Name property
- [x] Update test serialization fixtures

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

| Provider | Class | Protocol | Status | Details |
|----------|-------|----------|--------|---------|
| Asana | `AsanaClient` | OAuth2 | **Active** | Endpoints current |
| DigitalOcean | `DigitalOceanClient` | OAuth2 | **Active** | Endpoints current |
| Exact Online | `ExactOnlineClient` | OAuth2 | **Active** | Endpoints current |
| Facebook | `FacebookClient` | OAuth2 | **Active** | Updated to Graph API v25.0 |
| Fitbit | `FitbitClient` | OAuth2 | **Active** | Endpoints current |
| Foursquare | `FoursquareClient` | OAuth2 | **Deprecated** | v2 /users/self endpoint deprecated; use Foursquare Places API v3 |
| GitHub | `GitHubClient` | OAuth2 | **Active** | Fixed: Bearer header instead of token |
| Google | `GoogleClient` | OAuth2 | **Active** | Updated to v2/v3 endpoints, sub field |
| Instagram | `InstagramClient` | OAuth2 | **Dead** | Legacy Instagram API (api.instagram.com) shut down 2020; replaced by Instagram Graph API under Meta |
| LinkedIn | `LinkedInClient` | OAuth2 | **Dead** | v1 API (/uas/oauth2/, /v1/people/~) and XML format shut down 2019; needs v2 REST API rewrite |
| Login Cidadão | `LoginCidadaoClient` | OAuth2 | **Unknown** | Niche Brazilian government identity provider; unable to verify current status |
| Mail.Ru | `MailRuClient` | OAuth2 | **Active** | Endpoints current |
| Microsoft | `MicrosoftClient` | OAuth2 | **Active** | NEW — Microsoft Identity Platform v2.0 + Graph v1.0 |
| Odnoklassniki | `OdnoklassnikiClient` | OAuth2 | **Active** | Endpoints current |
| Salesforce | `SalesforceClient` | OAuth2 | **Active** | Endpoints current |
| Spotify | `SpotifyClient` | OAuth2 | **Active** | Endpoints current |
| Todoist | `TodoistClient` | OAuth2 | **Deprecated** | Uses Sync API v6; current is Sync API v9 / REST API v2 |
| Twitter | `TwitterClient` | OAuth1 | **Active** | Rebranded to X; OAuth 1.0a endpoints still operational; API access tiers changed |
| Uber | `UberClient` | OAuth2 | **Active** | Endpoints current |
| VK | `VkClient` | OAuth2 | **Active** | Endpoints current |
| VSTS | `VSTSClient` | OAuth2 | **Active** | Rebranded to Azure DevOps; endpoints still operational |
| Windows Live | `WindowsLiveClient` | OAuth2 | **Dead** | Live Connect API retired Nov 2018; apis.live.net no longer returns data |
| Xing | `XingClient` | OAuth1 | **Dead** | API shut down after Xing rebranded to New Work SE |
| Yahoo | `YahooClient` | OAuth2 | **Active** | Endpoints current |
| Yandex | `YandexClient` | OAuth2 | **Active** | Endpoints current |

### Summary

- **4 Dead providers:** WindowsLive, Xing, LinkedIn (v1), Instagram (legacy)
- **2 Deprecated providers:** Foursquare (v2), Todoist (v6)
- **2 Rebranded:** Twitter → X, VSTS → Azure DevOps
- **1 Unknown:** Login Cidadão
- **16 Active providers:** No known issues
