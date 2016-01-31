using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Logging;
using Orchard.Security;
using Softea.ResourceForm.Models;

namespace Softea.ResourceForm.Handlers
{
    public class ResourceFormPartHandler : ContentHandler
    {
        readonly IEncryptionService encryptionService;

        public ResourceFormPartHandler(
            IRepository<ResourceFormPartRecord> repository,
            IEncryptionService encryptionService)
        {
            this.encryptionService = encryptionService;

            Filters.Add(StorageFilter.For(repository));
            Filters.Add(new ActivatingFilter<ResourceFormPart>("ResourceForm"));

            OnActivated<ResourceFormPart>(ComputedFieldsInitializerHandler);

            OnCreated<ResourceFormPart>(ModifiedHandler);
            OnUpdated<ResourceFormPart>(ModifiedHandler);
            OnRemoved<ResourceFormPart>(ModifiedHandler);
        }

        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            var part = context.ContentItem.As<ResourceFormPart>();

            if (part != null)
            {
                //context.Metadata.Identity.Add("ResourceForm.Name", part.Name);
                //context.Metadata.DisplayText = part.Name;
            }
        }

        void ComputedFieldsInitializerHandler(ActivatedContentContext context, ResourceFormPart part)
        {



        }

        void ModifiedHandler(ContentContextBase context, ResourceFormPart part)
        {

        }
    }
}