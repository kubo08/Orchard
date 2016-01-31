using System;
using Softea.DirectoryServices.Models;
using Softea.DirectoryServices.Services;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Logging;
using Orchard.Security;

namespace Softea.DirectoryServices.Handlers
{
    [UsedImplicitly]
    public class LdapDirectoryPartHandler : ContentHandler
    {
        readonly IEncryptionService encryptionService;
        readonly ILdapDirectoryCache ldapDirectoryCache;

        public LdapDirectoryPartHandler(
            IRepository<LdapDirectoryPartRecord> repository,
            IEncryptionService encryptionService,
            ILdapDirectoryCache ldapDirectoryCache)
        {
            this.encryptionService = encryptionService;
            this.ldapDirectoryCache = ldapDirectoryCache;

            Filters.Add(StorageFilter.For(repository));
            Filters.Add(new ActivatingFilter<LdapDirectoryPart>("LdapDirectory"));

            OnActivated<LdapDirectoryPart>(ComputedFieldsInitializerHandler);
            
            OnCreated<LdapDirectoryPart>(ModifiedHandler);
            OnUpdated<LdapDirectoryPart>(ModifiedHandler);
            OnRemoved<LdapDirectoryPart>(ModifiedHandler);
        }

        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            var part = context.ContentItem.As<LdapDirectoryPart>();

            if (part != null)
            {
                context.Metadata.Identity.Add("LdapDirectory.Name", part.Name);
                context.Metadata.DisplayText = part.Name;
            }
        }

        void ComputedFieldsInitializerHandler(ActivatedContentContext context, LdapDirectoryPart part)
        {
            Func<string> failureHandler = () =>
            {
                Logger.Error("ServiceAccountPassword could not be decrypted. It might be corrupted, try to reset it.");
                return null;
            };

            part.ServiceAccountPasswordField.Getter(() => PasswordUtils.DecodePassword(part.Record.ServiceAccountPassword, encryptionService, failureHandler));
            part.ServiceAccountPasswordField.Setter(v => part.Record.ServiceAccountPassword = PasswordUtils.EncodePassword(v, encryptionService));
        }

        void ModifiedHandler(ContentContextBase context, LdapDirectoryPart part)
        {
            if (context.ContentItem.ContentType == "LdapDirectory")
                ldapDirectoryCache.Invalidate();
        }
    }
}