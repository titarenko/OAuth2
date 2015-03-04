using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using OAuth2.Client;
using OAuth2.Example2.Models;

namespace OAuth2.Example2.Controllers
{
	public class HomeController : Controller
	{
		private readonly AuthorizationRoot authorizationRoot;

		private const string ProviderNameKey = "providerName";

		private string ProviderName
		{
			get { return (string)Session[ProviderNameKey]; }
			set { Session[ProviderNameKey] = value; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HomeController"/> class.
		/// </summary>
		/// <param name="authorizationRoot">The authorization manager.</param>
		public HomeController()
		{
			this.authorizationRoot = new AuthorizationRoot ();
		}

		/// <summary>
		/// Renders home page with login link.
		/// </summary>
		public ActionResult Index()
		{
			var model = authorizationRoot.Clients.Select(client => new LoginInfoModel
				{
					ProviderName = client.Name
				});
			return View(model);
		}

		/// <summary>
		/// Redirect to login url of selected provider.
		/// </summary>        
		public RedirectResult Login()
		{
			ProviderName = Request ["providerName"]; ;
			return new RedirectResult(GetClient().GetLoginLinkUri());
		}

		/// <summary>
		/// Renders information received from authentication service.
		/// </summary>
		public ActionResult Auth()
		{
			var client = GetClient ();


			return View(client.GetUserInfo(Request.QueryString));
		}

		private IClient GetClient()
		{
			return authorizationRoot.Clients.First();
		}
	}
}

