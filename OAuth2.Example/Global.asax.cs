using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using OAuth2.Client;
using OAuth2.Configuration;
using OAuth2.Example.Controllers;
using RestSharp;
using Autofac.Builder;

namespace OAuth2.Example
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            SetDependencyResolver();
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            var resolver = (AutofacDependencyResolver) DependencyResolver.Current;
            var sessionScope = resolver.ApplicationContainer.BeginLifetimeScope("session");
            Session["Autofac_LifetimeScope"] = sessionScope;
        }

        protected void Session_End(object sender, EventArgs e)
        {
            var sessionScope = (ILifetimeScope) Session["Autofac_LifetimeScope"];
            sessionScope.Dispose();
        }

        private void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        private void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });

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

        private void SetDependencyResolver()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModelBinders(Assembly.GetExecutingAssembly());
            builder.RegisterControllers(Assembly.GetExecutingAssembly());

            builder
                .RegisterAssemblyTypes(
                    Assembly.GetExecutingAssembly(),
                    Assembly.GetAssembly(typeof (Client.OAuth2Client)),
                    Assembly.GetAssembly(typeof (RestClient)))
                .AsImplementedInterfaces().AsSelf();

            builder.Register(
                context =>
                context
                    .Resolve<IConfigurationManager>()
                    .GetConfigSection<OAuth2ConfigurationSection>("oauth2")["WindowsLiveClient"]);

            //builder.Register(context =>
            //                 context.Resolve<LinkedinClient>());
                                 //{
                                 //    var sessionScope = (ILifetimeScope) Session["Autofac_LifetimeScope"];
                                 //    return sessionScope.Resolve<LinkedinClient>();
                                 //});

            DependencyResolver.SetResolver(new AutofacDependencyResolver(builder.Build()));
        }
    }
}