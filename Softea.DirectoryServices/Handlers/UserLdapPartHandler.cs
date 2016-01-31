using JetBrains.Annotations;
using Orchard.Data;
using Orchard.ContentManagement.Handlers;
using Softea.DirectoryServices.Models;

namespace Softea.DirectoryServices.Handlers
{
    [UsedImplicitly]
    public class UserLdapPartHandler : ContentHandler
    {
        public UserLdapPartHandler(IRepository<UserLdapPartRecord> repository)
        {
            Filters.Add(StorageFilter.For(repository));
            Filters.Add(new ActivatingFilter<UserLdapPart>("User"));
        }
    }
}