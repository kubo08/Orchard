using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using Softea.DirectoryServices.Models;

namespace Softea.DirectoryServices.Services
{
    public interface ILdapDirectoryCache : IDependency
    {
        ICollection<ILdapDirectory> GetDirectories(bool enabledOnly = true);
        ILdapDirectory GetDirectory(int directoryId, bool enabledOnly = true);
        void Invalidate();
    }
}