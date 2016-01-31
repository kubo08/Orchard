using System;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using Softea.DirectoryServices.ADWrapper.Converters;
using Softea.DirectoryServices.Models;
using LinqToLdap;
using LinqToLdap.Collections;
using Orchard.Security;
using System.Collections.Generic;

namespace Softea.DirectoryServices.Services
{
    // Instance members are not thread safe.
    // TODO supporting connection pooling
    // TODO maybe supporting failover and load balancing using replication servers (or just leave that to an NLB?)
    public class LdapService : ILdapService
    {
        protected class ConnectionHolder : IDisposable
        {
            readonly Action<LdapConnection> releaser;
            readonly Lazy<IDirectoryContext> directoryContext;

            public ConnectionHolder(Func<LdapConnection> acquirer, Action<LdapConnection> releaser)
            {
                Connection = acquirer();
                this.releaser = releaser;
                directoryContext = new Lazy<IDirectoryContext>(() => new DirectoryContext(Connection));
            }

            public void Dispose()
            {
                releaser(Connection);
            }

            public LdapConnection Connection { get; private set; }

            public IDirectoryContext DirectoryContext
            {
                get { return directoryContext.Value; }
            }
        }

        protected static void AssertSuccess(ModifyResponse response)
        {
            if (response == null)
                throw new LdapException("Incorrect response returned from server.");
            if (response.ResultCode != ResultCode.Success)
                throw new LdapException(string.Format("Modify request returned '{0}' with message '{1}'.", response.ResultCode, response.ErrorMessage));
        }

        LdapConfiguration configuration;
        ILdapConnectionFactoryConfiguration connectionFactoryConfiguration;

        public LdapService(ILdapDirectory directory)
        {
            Directory = directory;

            configuration = new LdapConfiguration();
            var server = Directory.Server;
            connectionFactoryConfiguration = configuration.ConfigureFactory(server.Host);

            if (server.UseSsl)
                connectionFactoryConfiguration.UseSsl(server.Port.Value);
            else
                connectionFactoryConfiguration.UsePort(server.Port.Value);

            if (server.UseUdp)
                connectionFactoryConfiguration.UseUdp();
        }

        protected ConnectionHolder GetConnectionForCore(NetworkCredential credential, AuthType authType = AuthType.Basic)
        {
            connectionFactoryConfiguration
                .AuthenticateBy(authType)
                .AuthenticateAs(credential);

            return new ConnectionHolder(connectionFactoryConfiguration.GetConnection, connectionFactoryConfiguration.ReleaseConnection);
        }

        protected ConnectionHolder GetConnectionFor(string userName, string password, IDirectoryAttributes userEntry = null, AuthType authType = AuthType.Basic)
        {

            if (userEntry == null)
                using (var holder = GetConnectionForServiceUser())
                {
                    userEntry = LookupUserEntry(holder, userName);
                    if (userEntry == null)
                        throw new ApplicationException(string.Format("No object was found with the user name {0}.", userName));
                }

            return GetConnectionForCore(new NetworkCredential(userEntry.DistinguishedName, password), authType);
        }

        protected ConnectionHolder GetConnectionForServiceUser()
        {
            string domain = null;
            string userName = Directory.ServiceAccountUserName;
            if (Directory.ServiceAccountUserName.Contains('\\'))
            {
                var userDomain = Directory.ServiceAccountUserName.Split('\\');
                domain = userDomain.FirstOrDefault();
                userName = userDomain.ElementAt(1);
            }
            return
                string.IsNullOrEmpty(Directory.ServiceAccountUserName) ?
                GetConnectionForCore(null, AuthType.Anonymous) :
                GetConnectionForCore(new NetworkCredential(userName, Directory.ServiceAccountPassword, domain));
        }

        bool AuthenticateCore(ConnectionHolder holder)
        {
            try
            {
                holder.Connection.Bind();
                return true;
            }
            catch (LdapException ex)
            {
                if (ex.ErrorCode == 49) // LDAP_INVALID_CREDENTIALS
                    return false;
                throw;
            }
        }

        public bool Authenticate(string userName, string password)
        {
            // binding (checking validity of password)
            bool authenticated;
            ConnectionHolder holder = null;
            try
            {
                try { holder = GetConnectionFor(userName, password); }
                catch (ApplicationException) { return false; }
                authenticated = AuthenticateCore(holder);
            }
            finally
            {
                if (holder != null)
                    holder.Dispose();
            }

            // enforcing filter if bind was successful and user filter was not applied earlier
            // (GetCredentialFor() has already done a lookup if Directory.BindUsingUserName == false)

            return authenticated;
        }

        protected IDirectoryAttributes LookupUserEntry(ConnectionHolder holder, string userName)
        {
            var query = holder.DirectoryContext
                .Query(Directory.BaseDn, objectClass: Directory.UserObjectClass)
                .Where(Directory.UserNameAttribute + '=' + userName);

            if (!string.IsNullOrEmpty(Directory.UserFilter))
                query = query.Where(Directory.UserFilter);

            IDirectoryAttributes result;
            try
            {
                result = query.SingleOrDefault();
            }
            catch (InvalidOperationException)
            {
                throw new ApplicationException(string.Format("More objects were found with the user name {0}.", userName));
            }

            return result;
        }

        protected virtual CreateUserParams CreateUserParams(string userName, string password, IDirectoryAttributes entry)
        {
            string email = null;
            //if (!entry.AttributeNames.Contains(Directory.UserEmailAttribute))
            //    throw new ApplicationException(string.Format("Attribute {0} was not found on the object.", Directory.UserEmailAttribute));

            //var email = entry.GetString(Directory.UserEmailAttribute);
            //if (string.IsNullOrEmpty(email))
            //    throw new ApplicationException(string.Format("Attribute {0} is not set on the object.", Directory.UserEmailAttribute));
            if (entry.AttributeNames.Contains(Directory.UserEmailAttribute))
            {
                email = entry.GetString(Directory.UserEmailAttribute);
            }

            return new CreateUserParams(userName, password, email, null, null, true);
        }

        public CreateUserParams GetCreateUserParams(string userName, string password)
        {
            using (var holder = GetConnectionForServiceUser())
            {
                var entry = LookupUserEntry(holder, userName);
                if (entry == null)
                    throw new ApplicationException(string.Format("No object was found with the user name {0}.", userName));

                return CreateUserParams(userName, password, entry);
            }
        }

        public bool ChangePassword(string userName, string oldPassword, string newPassword)
        {
            IDirectoryAttributes userEntry;

            using (var holder = GetConnectionForServiceUser())
            {
                userEntry = LookupUserEntry(holder, userName);
                if (userEntry == null)
                    throw new ApplicationException(string.Format("No object was found with the user name {0}.", userName));
            }

            using (var holder = GetConnectionFor(userName, oldPassword, userEntry))
            {
                if (!AuthenticateCore(holder))
                    return false;

                var request = new ModifyRequest(userEntry.DistinguishedName, DirectoryAttributeOperation.Replace, Directory.UserPasswordAttribute, newPassword);
                var response = holder.Connection.SendRequest(request) as ModifyResponse;
                AssertSuccess(response);
            }

            return true;
        }

        protected IEnumerable<IDirectoryAttributes> LookupUserEntries(ConnectionHolder holder)
        {
            var query = holder.DirectoryContext
                .Query(Directory.BaseDn, objectClass: Directory.UserObjectClass, objectCategory: Directory.UserObjectCategory);

            if (!string.IsNullOrEmpty(Directory.UserFilter))
                query = query.Where(Directory.UserFilter);

            IEnumerable<IDirectoryAttributes> result = query.ToList();

            return result;
        }

        public ILdapDirectory Directory { get; private set; }
    }
}