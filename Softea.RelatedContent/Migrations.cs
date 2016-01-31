using System;
using System.Collections.Generic;
using System.Data;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;

namespace Softea.RelatedContent
{
    public class Migrations : DataMigrationImpl
    {

        public int Create()
        {

            ContentDefinitionManager.AlterPartDefinition("RelatedContentPart", builder => builder.Attachable());
            return 1;
        }
    }
}