using System;
using System.Collections.Generic;
using System.DirectoryServices;
using Orchard;
using Orchard.Logging;
using Softea.DirectoryServices.ADWrapper.Converters;
using Softea.DirectoryServices.Models;

namespace Softea.DirectoryServices.ADWrapper
{
    public class ADServices
    {
        public ADServices(string LDAPDefault, string LDAPBase)
        {
            ADHelper.LDAPDefault = LDAPDefault;
            ADHelper.LDAPBase = LDAPBase;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets the LDAP users.
        /// </summary>
        /// <param name="directory">ldap directory path.</param>
        /// <param name="OUName">Name of the organization unit in ldap path.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Chyba pri vyhladavani pouzivatelov v ad. Pravdepodobne zla LDAP cesta.</exception>
        public SearchResultCollection GetLDAPUsers(ILdapDirectory directory, string OUName = null)
        {

            DirectoryEntry de = ADHelper.GetDirectoryEntry();
            var deSearch = new DirectorySearcher(OUName);
            deSearch.SearchRoot = de;
            string member = null;
            if (!String.IsNullOrEmpty(directory.UserMemberOf))
            {
                member = string.Format("(memberof={0},{1})", directory.UserMemberOf, directory.BaseDn);
            }
            deSearch.Filter = String.Format("(&(objectCategory={0})(objectClass={1}){2})", directory.UserObjectCategory, directory.UserObjectClass, member);
            SearchResultCollection result = null;
            try
            {
                result = deSearch.FindAll();
            }
            catch (Exception ex)
            {
                throw new Exception("Chyba pri vyhladavani pouzivatelov v ad. Pravdepodobne zla LDAP cesta.", ex);
            }
            return result;
        }

        public List<User> GetUsers(ILdapDirectory directory, string OUName = null)
        {
            var result = GetLDAPUsers(directory, OUName);
            var users = result.EntityToModel();
            return users;
        }
    }
}