using System.Linq;
using System.Web.Mvc;
using OAuth2.Client;
using OAuth2.Example.Models;
using System.Threading.Tasks;
using System;

namespace OAuth2.Example.Controllers
{
    /// <summary>
    /// The only controller in this example app.
    /// </summary>
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
        public HomeController(ExampleAuthorizationRoot authorizationRoot)
        {
            this.authorizationRoot = authorizationRoot;
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
        public async Task<RedirectResult> Login(string providerName)
        {
            ProviderName = providerName;
            return new RedirectResult(await GetClient().GetLoginLinkUri());
        }

        /// <summary>
        /// Renders information received from authentication service.
        /// </summary>
        public ActionResult Auth()
        {
            var kvp = Enumerable.Range(0, Request.QueryString.Count)
                .SelectMany(x => Request.QueryString.GetValues(x).Select(y => Tuple.Create(Request.QueryString.GetKey(x), y)))
                .ToLookup(x => x.Item1, x => x.Item2, StringComparer.OrdinalIgnoreCase);
            return View(GetClient().GetUserInfo(kvp));
        }

        private IClient GetClient()
        {
            return authorizationRoot.Clients.First(c => c.Name == ProviderName);
        }
    }
}