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

- Asana
- DigitalOcean
- ExactOnline
- Facebook
- Fitbit
- Foursquare
- GitHub
- Google
- Instagram
- LinkedIn
- LoginCidadao
- MailRu
- Odnoklassniki
- Salesforce
- Spotify
- Todoist
- Twitter
- Uber
- VK (Vkontakte)
- Visual Studio Team Services (VSTS)
- Windows Live
- Xing
- Yahoo
- Yandex

## Goals

- Simplicity in usage — even a newcomer can call a couple of methods and receive the expected result
- Well-documented, testable, and tested code
- Flexible, transparent, and easily understandable design
- Support for both fine-grained control and simple plug-and-play usage

## Dependencies

- [RestSharp](https://restsharp.dev)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)

## Contributors

- Constantin Titarenko (started development, defined library structure, released initial version)
- Blake Niemyjski (helped a lot to maintain the project, currently (since 2015) — top maintainer)
- [Andriy Somak](https://github.com/semack) (helped with improvements on configuration and extending the list of supported services)
- Sascha Kiefer (simplified extending the library with own provider implementations, added GitHub client)
- Krisztián Pócza (added LinkedIn (OAuth 2) client)
- [Jamie Houston](https://github.com/JamieHouston) (added a [Todoist client](OAuth2/Client/Impl/TodoistClient.cs))
- [Sasidhar Kasturi](https://github.com/skasturi) (added Uber, Spotify, Yahoo)
- [Jamie Dalton](https://github.com/daltskin) (added Visual Studio Team Services)

## License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).

Copyright (c) 2012-2013 Constantin Titarenko, Andrew Semack and others
