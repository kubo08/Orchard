using System.Collections.Generic;
using Softea.DirectoryServices.Models;

namespace Softea.DirectoryServices.ViewModels
{
    public class LdapDirectoriesIndexViewModel
    {
        public IList<LdapDirectoryPart> LdapDirectories { get; set; }
        public LdapDirectoryIndexOptions Options { get; set; }
        public dynamic Pager { get; set; }
    }

    public class LdapDirectoryIndexOptions
    {
        public string Search { get; set; }
        public LdapDirectoriesFilter Filter { get; set; }
    }

    public enum LdapDirectoriesFilter
    {
        All,
        Enabled,
        Disabled
    }
}
