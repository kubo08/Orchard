using System.Linq;
using System.Web.Mvc;
using Orchard.ContentManagement.MetaData;
using Softea.RelatedContent.Models;
using Softea.RelatedContent.ViewModels;

namespace Softea.RelatedContent.Controllers
{
    public class RelatedcontentController : Controller
    {
        private readonly IContentDefinitionManager contentDefinitionManager;

        public RelatedcontentController(IContentDefinitionManager contentDefinitionManager)
        {
            this.contentDefinitionManager = contentDefinitionManager;
        }

        // GET: Relatedcontent
        public ActionResult Index(PropertyFieldsViewModel data, string preffix, int? removeId, bool? Add)
        {
            var possiblecontentTypes = contentDefinitionManager.ListTypeDefinitions().ToList();
            if (removeId.HasValue && data.Fields.Count > 1)
                data.Fields.RemoveAt(removeId.Value);
            if (Add != null)
                data.Fields.Add(new RelationshipFieldsModel());
            foreach (var field in data.Fields)
            {
                field.PossiblecontentTypes = possiblecontentTypes;
            }
            data.Preffix = preffix;


            return PartialView(data);
        }
    }
}