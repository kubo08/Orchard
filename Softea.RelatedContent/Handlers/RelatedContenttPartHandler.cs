using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Softea.Relatedcontent.Models;
using Softea.Relatedcontent.Services;

namespace Softea.RelatedContent.Handlers
{
    public class CurrentContentPartHandler : ContentHandler
    {
        private readonly IRelatedContentService relatedContentService;

        public CurrentContentPartHandler(IRelatedContentService relatedContentService)
        {
            this.relatedContentService = relatedContentService;

            OnActivated<RelatedContentPart>(SetupLazyFields);
        }

        private void SetupLazyFields(ActivatedContentContext context, RelatedContentPart part)
        {
            part.RelatedContentTypesField.Loader(() => relatedContentService.GetRelatedContent(part));

        }
    }
}