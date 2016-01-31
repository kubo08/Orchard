using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Softea.DirectoryServices.Models
{
   public class User
    {
       public string LoginName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string SecondaryEmail { get; set; }
        public string PhoneNo { get; set; }
        public bool? Enabled { get; set; }
        public string Title { get; set; }
        public string JobTitle { get; set;}
        public string Office { get; set; }
        public string Department { get; set; }
        public byte[] UserID { get; set; }
        public byte[] Photo { get; set; }

        public string FullName
        {
            get { return String.Format("{0} {1} {2}", Surname, GivenName, Title); }
        }
    }
}
