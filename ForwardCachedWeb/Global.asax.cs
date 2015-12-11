using ForwardCachedWeb.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ForwardCachedWeb
{
    public class MvcApplication : System.Web.HttpApplication
    {

        public static string Host;
        public static string LogPath;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            ProxyHost.Init(Server.MapPath("/config/hosts.json"));
            LogPath = Server.MapPath("/logs");

            System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => { return true; };
        }
    }
}
