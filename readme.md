# OAuth2 #

OAuth2 is a library for doing authentication with help of third-party services such as Google API, Facebook API and so on using OAuth/OAuth2 protocol.

## Standard flow ##

Following are the steps of standard usage flow:

- generate login URL and render page with it
- define callback which will be called by third-party service on successful authentication
- retrieve user info on callback from third-party service

## Usage example ##

- Install OAuth2 package via NuGet

`Install-Package OAuth2`

- Configure library

	<configSections>
		<section name="oauth2" type="OAuth2.Configuration.OAuth2ConfigurationSection, OAuth2, Version=0.8.*, Culture=neutral"/>
	</configSections>

	<oauth2>		
		<services>
			<add clientType="GoogleClient"
				enabled="true"
				clientId="000000000000.apps.googleusercontent.com"
				clientSecret="AAAAAAAAAAAAAAAAAAAAAAAA"
				scope="https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email"
				redirectUri="~/auth" />
			<add clientType="WindowsLiveClient"
				enabled="false"
				clientId="AAAAAAAAAAAAAAA"
				clientSecret="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
				scope="wl.basic wl.emails"
				redirectUri="oauth2.example.local/auth" />
		</services>
	</oauth2>

- Instantiate AuthorizationRoot (use IoC container or do manual "newing" using default ctor)

	public RootController(AuthorizationRoot authorizationRoot)
	{
		this.authorizationRoot = authorizationRoot;
	}

	public RootController() : this(new AuthorizationRoot())
	{
	}

- Obtain login URL and render page with it

	public ActionResult Index()
	{
		var uri = authorizationRoot.Clients[0].GetLoginLinkUri();
		return View(uri);
	}

- Define action for receiving callback from third-party service

	public ActionResult Auth()
    {
		var info = authorizationRoot.Clients[0].GetUserInfo(Request.QueryString);
        return View(info);
    }

- Use user info as you wish, for example, display user details:

	@model OAuth2.Models.UserInfo

	<p>
		@if (@Model.PhotoUri.IsEmpty())
		{
			@:"No photo"
		}
		else
		{
			<img src="@Model.PhotoUri" alt="photo"/>
		}
	</p>
	<p>
		@Model.FirstName @Model.LastName (@Model.Email) [@Model.Id, @Model.ProviderName]
	</p>

## Supported Services ##

- Facebook
- Foursquare
- Google
- Instagram
- LinkedIn
- MailRu
- Odnoklassniki
- Twitter
- VK (Vkontakte)
- Windows Live
- Yandex

## Goals ##

Before I started working on this project I considered available solutions: several ones were found, but I wasn't satisfied with results:

- some of them were too complex for such simple task as authentication via OAuth2
- some - didn't have usage examples or documentation

So, I decided to implement this library striving to achieve following goals:

- simplicity - so even newbie can just call couple of methods and receive expected results
- well-documented, testable and tested (!) code - current coverage (according to NCrunch) is greater than 80%, several acceptance tests are also implemented (SpecFlow + WatiN)
- self-education :) - it was interesting to see how OAuth2 works

## Dependencies ##

This library is dependent on:

- RestSharp (http://restsharp.org/)
- Newtonsoft.Json (http://json.codeplex.com/)

## Contributors ##

- Constantin Titarenko (started development, defined library structure, released initial version)
- Andrew Semack (helped a lot with improvements on configuration, as well as with extending list of supported services by implementing their clients)

## License ##

The MIT License (MIT)
Copyright (c) 2012 Constantin Titarenko, Andrew Semack

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.