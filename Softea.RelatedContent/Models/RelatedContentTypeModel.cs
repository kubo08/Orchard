﻿using System.Collections.Generic;
using Orchard.ContentManagement;

namespace Softea.RelatedContent.Models
{
    public class RelatedContentTypeModel
    {
        public IList<ContentItem> RelatedItems { get; set; }

        public string Name { get; set; }
    }
}