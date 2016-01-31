using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Orchard.ContentManagement.MetaData.Models;

namespace Softea.RelatedContent.Settings
{
    public class RelatedContentRelationshipSettings
    {
        public string RelatedcontentType { get; set; }

        public string RelatedContentFields { get; set; }

        public string DisplayName { get; set; }

    }
}