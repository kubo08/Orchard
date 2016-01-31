using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Softea.DirectoryServices.Models;
using Softea.DirectoryServices.Services;
using Softea.DirectoryServices.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Settings;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;

namespace Softea.DirectoryServices.Controllers
{
    public class AdminController : Controller, IUpdateModel
    {
        const string ldapDirectoryPrefix = "LdapDirectory";

        readonly ISiteService siteService;
        readonly ILdapServiceFactory ldapDirectoryService;

        public AdminController(
            IOrchardServices services,
            IShapeFactory shapeFactory,
            ISiteService siteService,
            ILdapServiceFactory ldapDirectoryService,
            ILdapDirectoryCache directoryCache)
        {
            Services = services;
            ShapeFactory = shapeFactory;
            this.siteService = siteService;
            this.ldapDirectoryService = ldapDirectoryService;

            T = NullLocalizer.Instance;
        }

        dynamic ShapeFactory { get; set; }
        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        public ActionResult Index(LdapDirectoryIndexOptions options, PagerParameters pagerParameters)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to list LDAP directories")))
                return new HttpUnauthorizedResult();

            var pager = new Pager(siteService.GetSiteSettings(), pagerParameters);

            // default options
            if (options == null)
                options = new LdapDirectoryIndexOptions();

            var directories = Services.ContentManager
                .Query<LdapDirectoryPart, LdapDirectoryPartRecord>().Where(d=>d.Name!=null);    //hack ak sa vytvoria prazdne zaznamy, po predchadzajucich zmazaniach

            switch (options.Filter)
            {
                case LdapDirectoriesFilter.Enabled:
                    directories = directories.Where(d => d.Enabled);
                    break;
                case LdapDirectoriesFilter.Disabled:
                    directories = directories.Where(d => !d.Enabled);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(options.Search))
                directories = directories.Where(d => d.Name.Contains(options.Search) && d.Name !=null);

            var pagerShape = ShapeFactory.Pager(pager).TotalItemCount(directories.Count());

            directories = directories.OrderBy(d => d.Name);

            var results = directories
                .Slice(pager.GetStartIndex(), pager.PageSize)
                .ToList();

            var model = new LdapDirectoriesIndexViewModel
            {
                LdapDirectories = results,
                Options = options,
                Pager = pagerShape
            };

            // maintain previous route data when generating page links
            var routeData = new RouteData();
            routeData.Values.Add("Options.Filter", options.Filter);
            routeData.Values.Add("Options.Search", options.Search);

            pagerShape.RouteData(routeData);

            return View(model);
        }

        ViewResult ViewForCreateOrEdit(dynamic model, LdapDirectoryPart directory)
        {
            var editor = ShapeFactory.EditorTemplate(
                TemplateName: "Parts/LdapDirectory.CreateOrEdit",
                Model: directory,
                Prefix: ldapDirectoryPrefix);
            editor.Metadata.Position = "2";
            model.Content.Add(editor);

            // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
            return View((object)model);
        }

        public ActionResult Create()
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage LDAP directories")))
                return new HttpUnauthorizedResult();

            var directory = Services.ContentManager.New<LdapDirectoryPart>("LdapDirectory");

            dynamic model = Services.ContentManager.BuildEditor(directory);

            return ViewForCreateOrEdit(model, directory);
        }

        [HttpPost, ActionName("Create")]
        public ActionResult CreatePost()
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage LDAP directories")))
                return new HttpUnauthorizedResult();

            var directory = Services.ContentManager.New<LdapDirectoryPart>("LdapDirectory");
            TryUpdateModel(directory, ldapDirectoryPrefix);

            directory.Validate(ModelState, Services, T);
            if (directory.UpdatePeriod <1)
                directory.UpdatePeriod = 1;

            // this fires OnUpdated on ContentHandler, too
            dynamic model = Services.ContentManager.UpdateEditor(directory, this);

            if (!ModelState.IsValid)
            {
                Services.TransactionManager.Cancel();
                return ViewForCreateOrEdit(model, directory);
            }

            directory.Enabled = true;

            // this fires OnCreated on ContentHandler, too
            Services.ContentManager.Create(directory);

            Services.Notifier.Information(T("LDAP directory {0} created", directory.Name));

            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage LDAP directories")))
                return new HttpUnauthorizedResult();

            var directory = Services.ContentManager.Get<LdapDirectoryPart>(id);
            dynamic model = Services.ContentManager.BuildEditor(directory);

            return ViewForCreateOrEdit(model, directory);
        }

        [HttpPost, ActionName("Edit")]
        public ActionResult EditPost(int id)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage LDAP directories")))
                return new HttpUnauthorizedResult();

            var directory = Services.ContentManager.Get<LdapDirectoryPart>(id);
            var currentPassword = directory.Record.ServiceAccountPassword;
            TryUpdateModel(directory, ldapDirectoryPrefix);
            if (directory.UpdatePeriod <1)
                directory.UpdatePeriod = 1;

            directory.Validate(ModelState, Services, T);

            // this fires OnUpdated on ContentHandler, too
            dynamic model = Services.ContentManager.UpdateEditor(directory, this);

            if (!ModelState.IsValid)
            {
                Services.TransactionManager.Cancel();
                return ViewForCreateOrEdit(model, directory);
            }

            if (string.IsNullOrEmpty(Request.Form[ldapDirectoryPrefix + ".ServiceAccountPasswordChanged"]))
                directory.Record.ServiceAccountPassword = currentPassword;

            Services.Notifier.Information(T("LDAP directory {0} updated", directory.Name));

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage LDAP directories")))
                return new HttpUnauthorizedResult();

            var directory = Services.ContentManager.Get<LdapDirectoryPart>(id);

            if (directory != null)
            {
                // disconnecting users authenticated by this directory
                var userLdaps = Services.ContentManager
                    .Query<UserLdapPart>("User")
                    .Where<UserLdapPartRecord>(ul => ul.LdapDirectoryId == directory.Id)
                    .List();
                foreach (var userLdap in userLdaps)
                    userLdap.LdapDirectoryId = null;

                // removing directory
                // this fires OnRemoved on ContentHandler, too
                Services.ContentManager.Remove(directory.ContentItem);
                
                Services.Notifier.Information(T("LDAP directory {0} deleted", directory.Name));
            }

            return RedirectToAction("Index");
        }

        public ActionResult Enable(int id)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage LDAP directories")))
                return new HttpUnauthorizedResult();

            var directory = Services.ContentManager.Get<LdapDirectoryPart>(id);

            if (directory != null)
            {
                directory.Enabled = true;

                // this fires OnUpdated on ContentHandler, too
                Services.ContentManager.UpdateEditor(directory, this);

                Services.Notifier.Information(T("LDAP directory {0} enabled", directory.Name));
            }

            return RedirectToAction("Index");
        }

        public ActionResult Disable(int id)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage LDAP directories")))
                return new HttpUnauthorizedResult();

            var directory = Services.ContentManager.Get<LdapDirectoryPart>(id);

            if (directory != null)
            {
                directory.Enabled = false;

                // this fires OnUpdated on ContentHandler, too
                Services.ContentManager.UpdateEditor(directory, this);

                Services.Notifier.Information(T("LDAP directory {0} disabled", directory.Name));
            }

            return RedirectToAction("Index");
        }


        public void AddModelError(string key, LocalizedString errorMessage)
        {
            ModelState.AddModelError(key, errorMessage.ToString());
        }

        bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            return TryUpdateModel(model, prefix, includeProperties, excludeProperties);
        }
    }

}
