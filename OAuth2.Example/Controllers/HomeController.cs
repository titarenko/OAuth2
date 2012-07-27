using System.Collections.Generic;
using System.Web.Mvc;
using OAuth2.Client;
using OAuth2.Example.Models;
using System.Linq;
using OAuth2.Models;

namespace OAuth2.Example.Controllers
{
    /// <summary>
    /// The only controller in this example app.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly IEnumerable<IClient> clients;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="clients">The clients.</param>
        public HomeController(IEnumerable<IClient> clients)
        {
            this.clients = clients;
        }

        /// <summary>
        /// Renders home page with login link.
        /// </summary>
        public ActionResult Index()
        {
            return View(new IndexViewModel
            {
                LoginUris = clients.Select(x => x.GetAccessCodeRequestUri())
            });
        }

        /// <summary>
        /// Renders information received from authentication service.
        /// </summary>
        public ActionResult Auth(string code, string error)
        {
            foreach (var client in clients)
            {
                try
                {
                    return View(client.GetUserInfo(client.GetAccessToken(code, error)));
                }
                catch
                {
                    // this is bad - don't use such "common" catches - 
                    // but at the moment we do not have means to distinguish
                    // clients, so we just trying them one by one
                }
            }

            // can't believe we can be here, but to satisfy compiler we need to have this line
            return View(new UserInfo());
        }
    }
}