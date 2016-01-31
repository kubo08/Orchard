using LinqToLdap.Collections;
using Softea.DirectoryServices.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;

namespace Softea.DirectoryServices.ADWrapper.Converters
{
    public static class UserConverter
    {
        public static User EntityToModel(DirectoryEntry result)
        {
            return new User
            {
                GivenName = result.Properties["givenName"].Count > 0 ? (result.Properties["givenName"].Value != null ? result.Properties["givenName"].Value.ToString() : string.Empty) : null,
                Surname = result.Properties["sn"].Count > 0 ? result.Properties["sn"].Value != null ? result.Properties["sn"].Value.ToString() : string.Empty : null,
                LoginName = result.Properties["sAMAccountName"].Value != null ? result.Properties["sAMAccountName"].Value.ToString() : string.Empty,
                Title = result.Properties["personalTitle"].Count > 0 ? result.Properties["personalTitle"].Value != null ? result.Properties["personalTitle"].Value.ToString() : string.Empty : null,
                PhoneNo = result.Properties["telephoneNumber"].Count > 0 ? result.Properties["telephoneNumber"].Value != null ? result.Properties["telephoneNumber"].Value.ToString() : string.Empty : result.Properties["mobile"].Count > 0 ? result.Properties["mobile"].Value != null ? result.Properties["mobile"].Value.ToString() : string.Empty : null,
                Email = result.Properties["mail"].Count > 0 ? result.Properties["mail"].Value != null ? result.Properties["mail"].Value.ToString() : string.Empty : null,
                JobTitle = result.Properties["title"].Count > 0 ? result.Properties["title"].Value != null ? (string)result.Properties["title"].Value : string.Empty : null,
                Office = result.Properties["physicalDeliveryOfficeName"].Count > 0 ? result.Properties["physicalDeliveryOfficeName"].Value != null ? result.Properties["physicalDeliveryOfficeName"].Value.ToString() : null : null,
                SecondaryEmail = result.Properties["otherMailbox"].Count > 0 ? result.Properties["otherMailbox"].Value != null ? result.Properties["otherMailbox"].Value.ToString() : string.Empty : null,
                Department = result.Properties["department"].Count > 0 ? result.Properties["department"].Value != null ? result.Properties["department"].Value.ToString() : string.Empty : null,
                UserID = ((byte[])(result.Properties["objectSid"].Value)),//result.Properties["objectSid"].Count>0?result.Properties["objectSid"].ToString():null,
                //512=Enabled
                //514= Disabled
                //66048 = Enabled, password never expires
                //66050 = Disabled, password never expires
                Enabled = result.Properties["useraccountcontrol"].Count > 0 ? (((int)result.Properties["useraccountcontrol"][0] == 512) || ((int)result.Properties["useraccountcontrol"][0] == 66048)) ? (bool?)true : false : null,
                Photo = (byte[])result.Properties["thumbnailPhoto"].Value
            };
        }

        public static List<User> EntityToModel(this SearchResultCollection result)
        {
            if (result == null)
                return new List<User>();

            List<User> users = new List<User>();
            foreach (SearchResult entry in result)
            {
                users.Add(EntityToModel(entry.GetDirectoryEntry()));
            }

            return users.OrderBy(t => t.Surname).ToList();
        }

        public static User EntityToModel(this SearchResult result)
        {
            var user = EntityToModel(result.GetDirectoryEntry());
            return user;
        }
    }
}
