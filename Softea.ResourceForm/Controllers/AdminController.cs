using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Settings;
using Softea.ResourceForm.Models;
using Softea.ResourceForm.ViewModels;

namespace Softea.ResourceForm.Controllers
{
    public class AdminController : Controller, IUpdateModel
    {
        public AdminController(
            IOrchardServices services)
        {
            Services = services;

            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        // GET: Admin
        [HttpGet]
        public ActionResult Index()
        {
            ResourceFormPart model;
            var items = Services.ContentManager.Query<ResourceFormPart>("ResourceForm").List();
            model = items.FirstOrDefault();
            if (model == null)
            {
                model = Services.ContentManager.New<ResourceFormPart>("ResourceForm");
                Services.ContentManager.Create(model);
            }
            return View(model);
        }

        [HttpPost, ActionName("Index")]
        public ActionResult IndexPost()
        {
            var record = Services.ContentManager.New<ResourceFormPart>("ResourceForm");
            TryUpdateModel(record);
            var item = Services.ContentManager.Query<ResourceFormPart>("ResourceForm").List().FirstOrDefault();
            item.MasterEmail = record.MasterEmail;

            return View(record);
        }

        bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            return TryUpdateModel(model, prefix, includeProperties, excludeProperties);
        }

        public void AddModelError(string key, LocalizedString errorMessage)
        {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }
}