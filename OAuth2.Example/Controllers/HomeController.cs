using System;
using System.Collections.Generic;
using System.Web.Caching;
using System.Web.Mvc;
using OAuth2.Client;
using OAuth2.Example.Models;

namespace OAuth2.Example.Controllers
{
    /// <summary>
    /// The only controller in this example app.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly IClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public HomeController(LinkedinClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Renders home page with login link.
        /// </summary>
        public ActionResult Index()
        {
            Session.Add("client", client);
            var model = new List<LoginInfoModel>
            {
                new LoginInfoModel
                {
                    ProviderName = client.ProviderName,
                    LoginUri = client.GetLoginLinkUri()
                }
            };
            return View(model);
        }

        /// <summary>
        /// Renders information received from authentication service.
        /// </summary>
        public ActionResult Auth()
        {
            var client = (IClient) Session["client"];
            return View(client.GetUserInfo(Request.QueryString));
        }
    }
}