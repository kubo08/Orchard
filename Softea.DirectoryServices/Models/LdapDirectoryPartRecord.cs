using Orchard.ContentManagement.Records;

namespace Softea.DirectoryServices.Models
{
    public class LdapDirectoryPartRecord : ContentPartRecord
    {
        public virtual string Name { get; set; }
        public virtual string NormalizedName { get; set; }
        public virtual bool Enabled { get; set; }
        public virtual string Server { get; set; }
        public virtual string ServiceAccountUserName { get; set; }
        public virtual string ServiceAccountPassword { get; set; }
        public virtual string BaseDn { get; set; }
        public virtual string UserObjectClass { get; set; }
        public virtual string UserFilter { get; set; }
        public virtual string UserNameAttribute { get; set; }
        public virtual string UserPasswordAttribute { get; set; }
        public virtual string UserEmailAttribute { get; set; }

        public virtual string UserObjectCategory { get; set; }
        public virtual string UserMemberOf { get; set; }
        public virtual int UpdatePeriod { get; set; }
    }
}