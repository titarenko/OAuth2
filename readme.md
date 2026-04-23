# OAuth2

[![Build](https://github.com/titarenko/OAuth2/workflows/Build/badge.svg)](https://github.com/titarenko/OAuth2/actions)
[![CodeQL](https://github.com/titarenko/OAuth2/workflows/CodeQL/badge.svg)](https://github.com/titarenko/OAuth2/actions/workflows/codeql.yml)
[![NuGet](https://img.shields.io/nuget/v/OAuth2.svg?style=flat)](https://www.nuget.org/packages/OAuth2/)

OAuth2 is a library for user authentication using third-party services (OAuth/OAuth2 protocol) such as Google, Facebook and so on.

## Current Version and Status

Current version is 0.10.x. Despite several real-life projects being built with this library, we will change the status to "stable" only after a comprehensive set of acceptance tests is ready and runs successfully.

## Standard Flow

1. Generate a login URL and render a page with it
2. Define a callback endpoint that the third-party service redirects to after successful authentication
3. Retrieve user info on callback from the third-party service

## Installation

Install the [OAuth2 NuGet package](https://www.nuget.org/packages/OAuth2/):

```shell
dotnet add package OAuth2
```

## Usage Example (ASP.NET Core Minimal API)

```csharp
using System.Collections.Specialized;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Helper to create a GoogleClient instance
GoogleClient CreateGoogleClient()
{
    return new GoogleClient(new RequestFactory(), new ClientConfiguration
    {
        ClientId = app.Configuration["Google:ClientId"]!,
        ClientSecret = app.Configuration["Google:ClientSecret"]!,
        RedirectUri = "https://localhost:5001/auth/google/callback",
        Scope = "profile email"
    });
}

// Step 1: Redirect the user to Google's login page
app.MapGet("/auth/google", async () =>
{
    var client = CreateGoogleClient();
    var loginUri = await client.GetLoginLinkUriAsync();
    return Results.Redirect(loginUri);
});

// Step 2: Handle the callback after authentication
app.MapGet("/auth/google/callback", async (HttpContext context) =>
{
    var code = context.Request.Query["code"].ToString();
    if (string.IsNullOrEmpty(code))
        return Results.BadRequest("Missing authorization code.");

    var client = CreateGoogleClient();
    var userInfo = await client.GetUserInfoAsync(new NameValueCollection { { "code", code } });

    return Results.Ok(new
    {
        userInfo.Id,
        userInfo.FirstName,
        userInfo.LastName,
        userInfo.Email,
        AvatarUri = userInfo.AvatarUri?.ToString()
    });
});

app.Run();
```

## Supported Services

| Provider | Client Class | Status | API Version | Auth Endpoint | Last Verified | Docs |
|----------|-------------|--------|-------------|---------------|---------------|------|
| GitHub | `GitHubClient` | Active | Current | `github.com/login/oauth/authorize` | 2026-04-23 | [Docs](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps) |
| Google | `GoogleClient` | Active | v3 (userinfo) | `accounts.google.com/o/oauth2/v2/auth` | 2026-04-23 | [Docs](https://developers.google.com/identity/protocols/oauth2/web-server) |
| Facebook | `FacebookClient` | Active | Graph API v25.0 | `www.facebook.com/v25.0/dialog/oauth` | 2026-04-23 | [Docs](https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow) |
| Microsoft | `MicrosoftClient` | Active | Identity Platform v2.0 / Graph v1.0 | `login.microsoftonline.com/common/oauth2/v2.0/authorize` | 2026-04-23 | [Docs](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow) |
| Asana | `AsanaClient` | Active | — | | | [Docs](https://developers.asana.com/docs/oauth) |
| DigitalOcean | `DigitalOceanClient` | Active | — | | | [Docs](https://docs.digitalocean.com/reference/api/oauth-api/) |
| ExactOnline | `ExactOnlineClient` | Active | — | | | [Docs](https://developers.exactonline.com/) |
| Fitbit | `FitbitClient` | Active | — | | | [Docs](https://dev.fitbit.com/build/reference/web-api/authorization/) |
| Foursquare | `FoursquareClient` | **Deprecated** | v2 (deprecated) | | | [Docs](https://docs.foursquare.com/) |
| Instagram | `InstagramClient` | **Dead** | Legacy API (shut down 2020) | | | [Docs](https://developers.facebook.com/docs/instagram-platform) |
| LinkedIn | `LinkedInClient` | **Dead** | v1 API (shut down 2019) | | | [Docs](https://learn.microsoft.com/en-us/linkedin/shared/authentication/authorization-code-flow) |
| LoginCidadao | `LoginCidadaoClient` | Unknown | — | | | |
| MailRu | `MailRuClient` | Active | — | | | [Docs](https://api.mail.ru/docs/guides/oauth/) |
| Odnoklassniki | `OdnoklassnikiClient` | Active | — | | | [Docs](https://apiok.ru/en/ext/oauth/) |
| Salesforce | `SalesforceClient` | Active | — | | | [Docs](https://help.salesforce.com/s/articleView?id=sf.remoteaccess_oauth_web_server_flow.htm) |
| Spotify | `SpotifyClient` | Active | — | | | [Docs](https://developer.spotify.com/documentation/web-api/tutorials/code-flow) |
| Todoist | `TodoistClient` | **Deprecated** | Sync API v6 (deprecated) | | | [Docs](https://developer.todoist.com/guides/#authorization) |
| Twitter | `TwitterClient` | Active | OAuth 1.0a | | | [Docs](https://developer.x.com/en/docs/authentication/oauth-1-0a) |
| Uber | `UberClient` | Active | — | | | [Docs](https://developer.uber.com/docs/riders/guides/authentication/introduction) |
| VK (Vkontakte) | `VkClient` | Active | — | | | [Docs](https://dev.vk.com/en/api/access-token/authcode-flow-user) |
| VSTS | `VSTSClient` | Active | Azure DevOps (rebranded) | | | [Docs](https://learn.microsoft.com/en-us/azure/devops/integrate/get-started/authentication/oauth) |
| Windows Live | `WindowsLiveClient` | **Dead** | Live SDK (retired Nov 2018) | | | [Migration Guide](https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/migrating-from-live-sdk) |
| Xing | `XingClient` | **Dead** | OAuth 1.0a (API shut down) | | | |
| Yahoo | `YahooClient` | Active | — | | | [Docs](https://developer.yahoo.com/oauth2/guide/) |
| Yandex | `YandexClient` | Active | — | | | [Docs](https://yandex.com/dev/id/doc/en/codes/code-url) |

> **Dead providers** (WindowsLive, Xing, LinkedIn v1, Instagram legacy): These providers' APIs have been retired or shut down. The client classes are preserved for backward compatibility but may not function. Use the replacement APIs documented in each class's XML docs.
>
> **Deprecated providers** (Foursquare v2, Todoist v6): These providers' API versions are deprecated. The client classes still function but should be updated to use current API versions.
>
> **Note:** The `WindowsLiveClient` is preserved unchanged for backward compatibility. Use `MicrosoftClient` instead, which targets Microsoft Identity Platform v2.0 and Microsoft Graph. Be aware that user IDs differ between the two platforms.

## Goals

- Simplicity in usage — even a newcomer can call a couple of methods and receive the expected result
- Well-documented, testable, and tested code
- Flexible, transparent, and easily understandable design
- Support for both fine-grained control and simple plug-and-play usage

## Dependencies

- [RestSharp](https://restsharp.dev)

## Contributors

- Constantin Titarenko (started development, defined library structure, released initial version)
- Blake Niemyjski (helped a lot to maintain the project, currently (since 2015) — top maintainer)
- [Andriy Somak](https://github.com/semack) (helped with improvements on configuration and extending the list of supported services)
- Sascha Kiefer (simplified extending the library with own provider implementations, added GitHub client)
- Krisztián Pócza (added LinkedIn (OAuth 2) client)
- [Jamie Houston](https://github.com/JamieHouston) (added a [Todoist client](OAuth2/Client/Impl/TodoistClient.cs))
- [Sasidhar Kasturi](https://github.com/skasturi) (added Uber, Spotify, Yahoo)
- [Jamie Dalton](https://github.com/daltskin) (added Visual Studio Team Services)

## Acknowledgements

Many thanks to [JetBrains](https://www.jetbrains.com/) company for providing free OSS licenses
for [**ReSharper**](https://www.jetbrains.com/resharper/) and [**dotCover**](https://www.jetbrains.com/dotcover/) -
these tools allow us to work on this project with pleasure!

Also we glad to have opportunity to use free [**Teamcity**](https://www.jetbrains.com/teamcity/) CI server
provided by [Codebetter.com](http://codebetter.com/) and [JetBrains](https://www.jetbrains.com/) -
many thanks for supporting OSS!

OAuth2 optimization would never be so simple without YourKit .NET profiler!
We appreciate kind support of open source projects by YourKit LLC -
the creator of innovative and intelligent tools for profiling .NET [**YourKit .NET Profiler**](https://www.yourkit.com/.net/profiler/index.jsp)
and Java applications [YourKit Java Profiler](https://www.yourkit.com/java/profiler/index.jsp).

## License

The MIT License (MIT)
Copyright (c) 2012-2013 Constantin Titarenko, Andrew Semack and others

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
