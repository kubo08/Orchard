using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;

namespace Softea.DirectoryServices
{
    public class ActionOverrideConstraint : IRouteConstraint
    {
        HashSet<string> actionsToOverride;

        public ActionOverrideConstraint(params string[] actionsToOverride)
        {
            this.actionsToOverride = new HashSet<string>(actionsToOverride, StringComparer.InvariantCultureIgnoreCase);
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var action = values["action"];
            return action != null && actionsToOverride.Contains(action);
        }
    }
}