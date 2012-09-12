using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Caching;
using System.Web.Mvc;
using OAuth2.Client;
using OAuth2.Example.Models;
using OAuth2.Models;

namespace OAuth2.Example.Controllers
{
    /// <summary>
    /// The only controller in this example app.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly AuthorizationManager authorizationManager;

        private const string ProviderNameKey = "providerName";

        private string ProviderName
        {
            get { return (string)Session[ProviderNameKey]; }
            set { Session[ProviderNameKey] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public HomeController(AuthorizationManager authorizationManager)
        {
            this.authorizationManager = authorizationManager;
        }

        /// <summary>
        /// Renders home page with login link.
        /// </summary>
        public ActionResult Index()
        {
            var model = authorizationManager.Clients.Select(client => new LoginInfoModel
                {
                    ProviderName = client.ProviderName
                });
            return View(model);
        }

        /// <summary>
        /// Redirect to login url of selected provider.
        /// </summary>        
        public RedirectResult Login(string providerName)
        {
            this.ProviderName = providerName;
            var client = authorizationManager.Clients.First(c => c.ProviderName == providerName);
            return new RedirectResult(client.GetLoginLinkUri());
        }

        /// <summary>
        /// Renders information received from authentication service.
        /// </summary>
        public ActionResult Auth()
        {
            var client = authorizationManager.Clients.First(c => c.ProviderName == this.ProviderName);
            UserInfo userInfo = client.GetUserInfo(Request.QueryString);
            return View(userInfo);
        }
    }
}