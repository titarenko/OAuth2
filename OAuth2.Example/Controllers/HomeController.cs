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
            Session.Add("authorizationManager", authorizationManager);
            var model = authorizationManager.Clients.Select(client => new LoginInfoModel
                {
                    ProviderName = client.ProviderName,
                    LoginUri = client.GetLoginLinkUri()
                });
            return View(model);
        }

        /// <summary>
        /// Renders information received from authentication service.
        /// </summary>
        public ActionResult Auth()
        {
            var authorizationManager = (AuthorizationManager)Session["authorizationManager"];
            foreach (var client in authorizationManager.Clients)
            {
                try
                {
                    UserInfo userInfo = client.GetUserInfo(Request.QueryString);
                    return View(userInfo);
                }
                catch
                {
                    // this is bad - don't use such "common" catches - 
                    // but at the moment we do not have means to distinguish
                    // clients, so we just trying them one by one
                }
            }
            return View();                    
        }
    }
}