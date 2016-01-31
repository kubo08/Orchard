using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Softea.DirectoryServices
{
    public class ActionOverrideAttribute : ActionFilterAttribute
    {
        string area, controller, action;

        public ActionOverrideAttribute(string area, string controller)
        {
            if (area == null)
                throw new ArgumentNullException("area");
            if (controller == null)
                throw new ArgumentNullException("controller");

            this.area = area;
            this.controller = controller;
        }

        public ActionOverrideAttribute(string area, string controller, string action)
            : this(area, controller)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            
            this.action = action;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.RouteData.DataTokens["area"] = area;
            filterContext.RouteData.Values["area"] = area;
            filterContext.RouteData.Values["controller"] = controller;
            if (action != null)
                filterContext.RouteData.Values["action"] = action;
        }
    }
}