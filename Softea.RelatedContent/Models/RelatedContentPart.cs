using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;
using Softea.RelatedContent.Models;

namespace Softea.Relatedcontent.Models
{
    public class RelatedContentPart : ContentPart
    {
        internal LazyField<IEnumerable<RelatedContentTypeModel>> RelatedContentTypesField = new LazyField<IEnumerable<RelatedContentTypeModel>>();

        public IEnumerable<RelatedContentTypeModel> RelatedContentTypes
        {
            get { return RelatedContentTypesField.Value; }
        }
    }
}