
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace OAuth2.Example2
{
	public class MvcApplication : System.Web.HttpApplication
	{
		private void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });

			routes.MapRoute(
				"Login", // Route name
				"login/{providerName}", // URL with parameters
				new
				{
					controller = "Home",
					action = "Login",
					id = UrlParameter.Optional
				});


			routes.MapRoute(
				"Auth", // Route name
				"Auth", // URL with parameters
				new
				{
					controller = "Home",
					action = "Auth",
					id = UrlParameter.Optional
				});

			routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
				new
				{
					controller = "Home",
					action = "Index",
					id = UrlParameter.Optional
				});
		}

		public static void RegisterGlobalFilters (GlobalFilterCollection filters)
		{
			filters.Add (new HandleErrorAttribute ());
		}

		protected void Application_Start ()
		{
			AreaRegistration.RegisterAllAreas ();
			RegisterGlobalFilters (GlobalFilters.Filters);
			RegisterRoutes (RouteTable.Routes);
		}
	}
}
