using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Navigation;

namespace Softea.ResourceForm
{
    public class AdminMenu : INavigationProvider
    {
        public string MenuName
        {
            get { return "admin"; }
        }

        public AdminMenu()
        {
            T = NullLocalizer.Instance;
        }

        private Localizer T { get; set; }

        public void GetNavigation(NavigationBuilder builder)
        {
            builder

                // Image set
                .AddImageSet("webshop")

                // "Webshop"
                .Add(item => item

                    .Caption(T("Resource Form"))
                    .Position("11")
                    .Action("Index", "Admin", new { area = "Softea.ResourceForm" })
                );
        }
    }
}