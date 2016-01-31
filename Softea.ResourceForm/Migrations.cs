using Orchard;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;

namespace Softea.ResourceForm
{
    public class Migrations : DataMigrationImpl
    {
        public IOrchardServices Services { get; set; }

        public Migrations(IOrchardServices services)
        {
            Services = services;
        }
        public int Create()
        {
            SchemaBuilder.CreateTable("ResourceFormPartRecord",
                table => table
                    .ContentPartRecord()
                    .Column<string>("MasterEmail")
                );

            Services.ContentManager.New("ResourceForm");

            return 1;
        }

        public int UpdateFrom1()
        {


            return 2;
        }
    }
}