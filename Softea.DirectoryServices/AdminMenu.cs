using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Navigation;

namespace Softea.DirectoryServices
{
    public class AdminMenu : INavigationProvider
    {
        public Localizer T { get; set; }
        public string MenuName { get { return "admin"; } }

        public void GetNavigation(NavigationBuilder builder)
        {
            builder
                // .AddImageSet("ldapDirectory") TODO
                .Add(
                    T("LDAP Directories"),
                    "10",
                    menu => menu
                        .Action("Index", "Admin", new { area = "Softea.DirectoryServices" })
                        .Permission(StandardPermissions.SiteOwner)
                        .Add(
                            T("LDAP Directories"),
                            "1.0",
                            item => item
                                .Action("Index", "Admin", new { area = "Softea.DirectoryServices" })
                                .LocalNav()
                                .Permission(StandardPermissions.SiteOwner)));
        }
    }
}
