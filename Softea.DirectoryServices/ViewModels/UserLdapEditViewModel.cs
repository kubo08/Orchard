using System.Collections.Generic;
using System.Web.Mvc;

namespace Softea.DirectoryServices.ViewModels
{
    public class UserLdapEditViewModel
    {
        public IEnumerable<SelectListItem> Directories { get; set; }
        public int? CurrentDirectoryId { get; set; }
    }
}