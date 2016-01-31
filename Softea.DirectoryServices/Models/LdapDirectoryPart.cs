using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Utilities;
using Orchard.Localization;

namespace Softea.DirectoryServices.Models
{
    public sealed class LdapDirectoryPart : ContentPart<LdapDirectoryPartRecord>, ILdapDirectory
    {
        public const string NameDisplayName = "Directory Name";
        public const string BaseDnDisplayName = "Base DN";
        public const string ServiceAccountUserNameDisplayName = "Service User Name";
        public const string ServiceAccountPasswordDisplayName = "Service User Password";
        public const string UserObjectClassDisplayName = "Object Class of User Type";
        public const string UserFilterDisplayName = "Additional Filter for Users";
        public const string UserNameAttributeDisplayName = "Unique Name Attribute of User Type";
        public const string UserPasswordAttributeDisplayName = "Password Attribute of User Type";
        public const string UserEmailAttributeDisplayName = "Email Attribute of User Type";
        public const string UserObjectCategoryDisplayName = "Object Category of User Type";
        public const string UserMemberOfDisplayName = "Groups of User Type";
        public const string UpdatePeriodDisplayName = "Update period (minutes)";

        int ILdapDirectory.Id
        {
            get { return Record.Id; }
        }

        [DisplayName(NameDisplayName)]
        [Required]
        public string Name
        {
            get { return Record.Name; }
            set { Record.Name = value; }
        }

        public bool Enabled
        {
            get { return Record.Enabled; }
            set { Record.Enabled = value; }
        }

        public LdapServer Server
        {
            get { return new LdapServer(Record.Server); }
            set { Record.Server = value.ToString(); }
        }

        [DisplayName(BaseDnDisplayName)]
        [Required]
        // TODO regex validator
        public string BaseDn
        {
            get { return Record.BaseDn; }
            set { Record.BaseDn = value; }
        }

        [DisplayName(ServiceAccountUserNameDisplayName)]
        public string ServiceAccountUserName
        {
            get { return Record.ServiceAccountUserName; }
            set { Record.ServiceAccountUserName = value; }
        }

        readonly ComputedField<string> serviceAccountPasswordField = new ComputedField<string>();
        public ComputedField<string> ServiceAccountPasswordField
        {
            get { return serviceAccountPasswordField; }
        }

        [DisplayName(ServiceAccountPasswordDisplayName)]
        [DataType(DataType.Password)]
        public string ServiceAccountPassword
        {
            get { return serviceAccountPasswordField.Value; }
            set { serviceAccountPasswordField.Value = value; }
        }

        [DisplayName(UserObjectClassDisplayName)]
        [Required]
        public string UserObjectClass
        {
            get { return Record.UserObjectClass; }
            set { Record.UserObjectClass = value; }
        }

        [DisplayName(UserFilterDisplayName)]
        public string UserFilter
        {
            get { return Record.UserFilter; }
            set { Record.UserFilter = value; }
        }

        [DisplayName(UserNameAttributeDisplayName)]
        [Required]
        public string UserNameAttribute
        {
            get { return Record.UserNameAttribute; }
            set { Record.UserNameAttribute = value; }
        }

        [DisplayName(UserPasswordAttributeDisplayName)]
        [Required]
        public string UserPasswordAttribute
        {
            get { return Record.UserPasswordAttribute; }
            set { Record.UserPasswordAttribute = value; }
        }

        [DisplayName(UserEmailAttributeDisplayName)]
        [Required]
        public string UserEmailAttribute
        {
            get { return Record.UserEmailAttribute; }
            set { Record.UserEmailAttribute = value; }
        }

        [DisplayName(UserObjectCategoryDisplayName)]
        [Required]
        public string UserObjectCategory
        {
            get { return Record.UserObjectCategory; }
            set { Record.UserObjectCategory = value; }
        }

        [DisplayName(UserMemberOfDisplayName)]
        public string UserMemberOf
        {
            get { return Record.UserMemberOf; }
            set { Record.UserMemberOf = value; }
        }

        [DisplayName(UpdatePeriodDisplayName)]
        public int UpdatePeriod
        {
            get { return Record.UpdatePeriod; }
            set { Record.UpdatePeriod = value; }
        }

        public void Validate(ModelStateDictionary modelState, IOrchardServices services, Localizer T)
        {
            // checking if name is unique
            ModelState nameState;
            if (!modelState.TryGetValue("Name", out nameState) || !nameState.Errors.Any())
            {
                var normalizedName = (Name ?? string.Empty).ToLowerInvariant();
                var directories = services.ContentManager
                    .Query<LdapDirectoryPart, LdapDirectoryPartRecord>()
                    .Where(d => d.NormalizedName == normalizedName)
                    .List();
                if ((ContentItem.Record == null && directories.Any()) ||
                    (ContentItem.Record != null && directories.Any(d => d.Id != Id)))
                    modelState.AddModelError("Name", T("LDAP directory with that name already exists.").Text);
            }
        }
    }
}