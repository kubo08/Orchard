using System;
using System.Collections.Generic;
using System.Data;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Data;
using Orchard.Security;
using Orchard.Logging;

namespace Softea.DirectoryServices
{
    public class DirectoryServicesMigration : DataMigrationImpl
    {


        readonly ISessionLocator sessionLocator;
        readonly IEncryptionService encryptionService;
        readonly ITransactionManager transactionmanager;

        public DirectoryServicesMigration(
            ISessionLocator sessionLocator,
            IEncryptionService encryptionService,
            ITransactionManager transactionManager)
        {
            this.sessionLocator = sessionLocator;
            this.encryptionService = encryptionService;
            this.transactionmanager = transactionManager;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public int Create()
        {
            SchemaBuilder.CreateTable("UserLdapPartRecord",
                table => table
                    .ContentPartRecord()
                    .Column<int>("LdapDirectoryId")
                );

            SchemaBuilder.CreateTable("LdapDirectoryPartRecord",
                table => table
                    .ContentPartRecord()
                    .Column<string>("Name")
                    .Column<string>("NormalizedName")
                    .Column<bool>("Enabled")
                    .Column<string>("Server")
                    .Column<string>("ServiceAccountUserName")
                    .Column<string>("ServiceAccountPassword")
                    .Column<string>("BaseDn")
                    .Column<string>("UserObjectClass")
                    .Column<string>("UserNameAttribute")
                    .Column<string>("UserPasswordAttribute")
                    .Column<string>("UserEmailAttribute")
                    .Column<string>("UserObjectCategory")
                    .Column<string>("UserMemberOf")
                );

            ContentDefinitionManager.AlterTypeDefinition("LdapDirectory", cfg => cfg.Creatable(false));

            return 1;
        }

        public int UpdateFrom1()
        {
            // encrypting ServiceAccountPassword of existing directories
            try
            {
                //var othersession = sessionLocator.For(typeof(LdapDirectoryPartRecord));
                var session = transactionmanager.GetSession();
                var tableName = Feature.Descriptor.Extension.Id.Replace(".", "_") + "_LdapDirectoryPartRecord";
                var selectQuery = session.CreateSQLQuery(string.Format("SELECT Id, ServiceAccountPassword FROM {0}", tableName));
                foreach (object[] row in selectQuery.List())
                {
                    var id = row[0];
                    var serviceAccountPassword = (string)row[1];
                    serviceAccountPassword = PasswordUtils.EncodePassword(serviceAccountPassword, encryptionService);
                    var updateQuery = session.CreateSQLQuery(string.Format("UPDATE {0} SET ServiceAccountPassword = :serviceAccountPassword WHERE Id = :id", tableName))
                        .SetParameter("serviceAccountPassword", serviceAccountPassword)
                        .SetParameter("id", id);
                    updateQuery.ExecuteUpdate();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Passwords of LDAP directories could not be updated by migration. You have to reset them manually on the UI. Details: {0}", ex);
            }

            return 2;
        }

        public int UpdateFrom2()
        {
            SchemaBuilder.AlterTable("LdapDirectoryPartRecord",
                table => table
                    .AddColumn<string>("UserFilter", col => col.Unlimited())
                );

            return 3;
        }

        public int UpdateFrom3()
        {
            SchemaBuilder.AlterTable("LdapDirectoryPartRecord",
                table => table
                    .AddColumn<int>("UpdatePeriod", col => col.WithDefault(1))
                );
            return 4;
        }
    }
}