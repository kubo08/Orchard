using System.Collections.Generic;
using Softea.RelatedContent.Models;

namespace Softea.RelatedContent.ViewModels
{
    public class PropertyFieldsViewModel
    {
        public IList<RelationshipFieldsModel> Fields { get; set; }

        public string Preffix { get; set; }
    }
}