using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ArtZada
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "ClientPortal",
                url: "client/{action}/{id}",
                defaults: new { controller = "Client", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "SellerPortal",
                url: "seller/{action}/{id}",
                defaults: new { controller = "Seller", action = "Store", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "AdminPortal",
                url: "admin/{action}/{id}",
                defaults: new { controller = "Admin", action = "Landing", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Root",
                url: "",
                defaults: new { controller = "Landing", action = "Landing" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Landing", action = "Landing", id = UrlParameter.Optional }
            );
        }
    }
}
