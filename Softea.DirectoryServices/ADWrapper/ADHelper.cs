using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;

namespace Softea.DirectoryServices.ADWrapper
{
    internal static class ADHelper
    {

        internal static string LDAPDefault { get; set; }
        internal static string LDAPBase { get; set; }

        #region essentials
        public static PrincipalContext GetPrincipalContext()
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain);

            return context;
        }

        public static DirectoryEntry GetDirectoryEntry()
        {
            DirectoryEntry de = new DirectoryEntry();
            de.Path = LDAPDefault;

            return de;
        }

        public static DirectoryEntry GetDirectoryEntry(string OUName)
        {
            DirectoryEntry de = new DirectoryEntry();
            if (String.IsNullOrEmpty(OUName))
                de.Path = LDAPDefault;
            else
                de.Path = LDAPBase + OUName;

            return de;
        }

        #endregion
    }
}
