using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Mvc.Routes;
using System.Web.Routing;
using System.Web.Mvc;
using Orchard;
using Orchard.ContentManagement;
using Softea.DirectoryServices.Models;
using Orchard.Data;

namespace Softea.DirectoryServices
{
    public class Routes : IRouteProvider
    {
        public IEnumerable<RouteDescriptor> GetRoutes()
        {
            return new[]
            {
                new RouteDescriptor 
                {
                    Name = "Softea.DirectoryServices.OrchardUsersAccountOverride",
                    Priority = 13,
                    Route = new Route("Users/Account/{action}/{id}",
                        new RouteValueDictionary
                        {
                            { "area", "Softea.DirectoryServices" },
                            { "controller", "OrchardUsersAccountOverride" },
                            { "id", UrlParameter.Optional },
                        },
                        new RouteValueDictionary 
                        {
                            { "action", new ActionOverrideConstraint("ChangePassword", "RequestLostPassword") }
                        },
                        new RouteValueDictionary
                        {
                            { "area", "Softea.DirectoryServices" }
                        },
                        new MvcRouteHandler())
                }                   
            };
        }

        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            foreach (var route in GetRoutes())
                routes.Add(route);
        }
    }
}