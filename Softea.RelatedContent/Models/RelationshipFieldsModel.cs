using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Orchard.ContentManagement.MetaData.Models;

namespace Softea.RelatedContent.Models
{
    public class RelationshipFieldsModel
    {
        public string RelatedcontentType { get; set; }

        public string RelatedContentFields { get; set; }

        public string DisplayName { get; set; }


        public IList<ContentTypeDefinition> PossiblecontentTypes { get; set; }

        public IEnumerable<SelectListItem> possibleContentTypesListItems
        {
            get
            {
                return PossiblecontentTypes.Select(f => new SelectListItem
                {
                    Value = f.Name.ToString(CultureInfo.InvariantCulture),
                    Text = f.DisplayName,
                    Selected = f.Name == RelatedcontentType
                });
            }
        }
    }
}