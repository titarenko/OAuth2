# OAuth2 #

OAuth2 is a library intended for user authentication using third-party services such as Google API, Facebook API and so on.

## Standard flow ##

Following are the steps of standard usage flow:

- client instance generates URI for login link (`GetAccessCodeRequestUri` method)
- hosting app renders page with login link using aforementioned URI
- user clicks login link - this leads to redirect to third-party service site
- user does authentication and allows app access his/her basic information
- third-party service redirects user to hosting app
- hosting app reads user information using `GetUserInfo` method on callback

## Usage example ##

Everything you need is:

- Install OAuth2 package via NuGet (`Install-Package OAuth2`)
- Obtain and render link (once user clicks it, they will be redirected to third-party authentication service)
- Receive callback from third-party service and read information about user (name, email, photo URI and unique identifier within third-party service)

Controller:

	public class HomeController : Controller
	{
	    private readonly IClient client;
	
	    /// <summary>
	    /// Initializes a new instance of the <see cref="HomeController"/> class.
	    /// </summary>
	    /// <param name="client">The client.</param>
	    public HomeController(IClient client)
	    {
	        this.client = client;
	    }
	
	    /// <summary>
	    /// Renders home page with login link.
	    /// </summary>
	    public ActionResult Index()
	    {
	        return View(client.GetAccessCodeRequestUri());
	    }
	
	    /// <summary>
	    /// Renders information received from authentication service.
	    /// </summary>
	    public ActionResult Auth(string code, string error)
	    {
	        return View(client.GetUserInfo(client.GetAccessToken(code, error)));
	    }
	}

RegisterRoutes (Global.asax.cs):

	routes.MapRoute(
        "Auth", // Route name
        "Auth", // URL with parameters
        new
        {
            controller = "Home",
            action = "Auth",
            id = UrlParameter.Optional
        });

Auth View:

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
	    @Model.FirstName @Model.LastName (@Model.Email) [@Model.Id]
	</p>

AppSettings (Web.config):

  	<appSettings>
		...

		<add key="RedirectUri" value="http://localhost:53023/Auth"/>
		
		<add key="GoogleClient.ClientId" value="000000000000.apps.googleusercontent.com"/>
		<add key="GoogleClient.ClientSecret" value="AAAAAAAAAAAAAAAAAAAAAAAA"/>
		<add key="GoogleClient.Scope" value="https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email"/>
		
		<add key="FacebookClient.ClientId" value="000000000000000"/>
		<add key="FacebookClient.ClientSecret" value="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"/>
		<add key="FacebookClient.Scope" value="email"/>
		
		<add key="VkClient.ClientId" value="0000000"/>
		<add key="VkClient.ClientSecret" value="AAAAAAAAAAAAAAAAAAAA"/>
		<add key="VkClient.Scope" value="offline"/>
	</appSettings>

## Supported Sevices##

Currently OAuth2 supports receiving user information via:

- Google
- Facebook
- VK (Vkontakte)

## Goals ##

Before I started working on this project I considered available solutions: several ones were found, but I wasn't satisfied with results:

- some of them were too complex for such simple task as authentication via OAuth2
- some - didn't have usage examples or documentation

So, I decided to implement this library striving to achieve next goals:

- simplicity - so even newbie can just call couple of methods and receive expected results
- self-education :) - it was interesting to see how OAuth2 works
- well-documented, testable and tested (!) code - current coverage (according to dotCover) is about 100%

## Dependencies ##

This library is dependent on:

- RestSharp (http://restsharp.org/)
- Newtonsoft.Json (http://json.codeplex.com/)

## License ##

The MIT License (MIT)
Copyright (c) 2012 Constantin Titarenko

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.