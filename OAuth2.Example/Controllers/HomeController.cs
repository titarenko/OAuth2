using System.Web.Mvc;

namespace OAuth2.Example.Controllers
{
    public class HomeController : Controller
    {
        private readonly Client.Client client;

        public HomeController(Client.Client client)
        {
            this.client = client;
        }

        public ActionResult Index()
        {
            return View((object) client.GetAccessCodeRequestUri());
        }

        public ActionResult Auth(string code, string error)
        {
            return View(client.GetUserInfo(client.GetAccessToken(code, error)));
        }
    }
}