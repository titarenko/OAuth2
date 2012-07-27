using System.Web.Mvc;

namespace OAuth2.Example.Controllers
{
    /// <summary>
    /// The only controller in this example app.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly Client.Client client;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public HomeController(Client.Client client)
        {
            this.client = client;
        }

        /// <summary>
        /// Renders home page with login link.
        /// </summary>
        public ActionResult Index()
        {
            return View((object) client.GetAccessCodeRequestUri());
        }

        /// <summary>
        /// Renders information received from authentication service.
        /// </summary>
        public ActionResult Auth(string code, string error)
        {
            return View(client.GetUserInfo(client.GetAccessToken(code, error)));
        }
    }
}