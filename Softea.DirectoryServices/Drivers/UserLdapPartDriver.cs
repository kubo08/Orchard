using System.Linq;
using System.Web.Mvc;
using Softea.DirectoryServices.Models;
using Softea.DirectoryServices.Services;
using Softea.DirectoryServices.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using Orchard.Security;

namespace Softea.DirectoryServices.Drivers
{
    public class UserLdapPartDriver : ContentPartDriver<UserLdapPart>
    {
        readonly IAuthenticationService authenticationService;
        readonly IAuthorizationService authorizationService;
        readonly ILdapDirectoryCache ldapDirectoryCache;

        public UserLdapPartDriver(
            IAuthenticationService authenticationService,
            IAuthorizationService authorizationService,
            ILdapDirectoryCache ldapDirectoryCache)
        {
            this.authenticationService = authenticationService;
            this.authorizationService = authorizationService;
            this.ldapDirectoryCache = ldapDirectoryCache;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        protected override string Prefix
        {
            get { return "UserLdap"; }
        }

        protected override DriverResult Editor(UserLdapPart part, dynamic shapeHelper)
        {
            if (!authorizationService.TryCheckAccess(StandardPermissions.SiteOwner, authenticationService.GetAuthenticatedUser(), part))
                return null;

            return ContentShape("Parts_Ldap_UserLdap_Edit", () =>
                shapeHelper.EditorTemplate(TemplateName: "Parts/Ldap.UserLdap", Model: BuildEditorViewModel(part), Prefix: Prefix));
        }

        protected override DriverResult Editor(UserLdapPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            if (!authorizationService.TryCheckAccess(StandardPermissions.SiteOwner, authenticationService.GetAuthenticatedUser(), part))
                return null;

            var model = new UserLdapEditViewModel();
            if (updater.TryUpdateModel(model, Prefix, null, null))
                part.LdapDirectoryId = model.CurrentDirectoryId;

            return Editor(part, shapeHelper);
        }             

        UserLdapEditViewModel BuildEditorViewModel(UserLdapPart part)
        {
            return new UserLdapEditViewModel
            {
                Directories = new[] { new SelectListItem { Value = string.Empty, Text = T("Orchard").Text, Selected = part.LdapDirectoryId.HasValue } }
                    .Concat(ldapDirectoryCache.GetDirectories(false).Select(d => new SelectListItem { Value = d.Id.ToString(), Text = T("LDAP Directory '{0}'", d.Name).Text, Selected = d.Id == part.LdapDirectoryId })),
                CurrentDirectoryId = part.LdapDirectoryId
            };
        }
    }
}