using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;

namespace Softea.DirectoryServices.Models
{
    public sealed class UserLdapPart : ContentPart<UserLdapPartRecord>
    {
        public int? LdapDirectoryId
        {
            get { return Record.LdapDirectoryId; }
            set { Record.LdapDirectoryId = value; }
        }

        // not persisted, for work around purposes only (see OrchardUsersAccountOverrideController.SetPassword)
        public string CurrentPassword { get; set; }
    }
}