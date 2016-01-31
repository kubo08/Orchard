using System;
using System.Collections.Generic;
using System.Linq;
using Softea.DirectoryServices.Models;
using Orchard;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.Environment.Configuration;
using Orchard.Environment.Descriptor;
using Orchard.Environment.State;
using Orchard.Events;
using Orchard.Security;

namespace Softea.DirectoryServices.Services
{
    public interface ILdapDirectoryCacheInvalidator : IEventHandler
    {
        void Invalidate(Action invalidator);
    }

    public class LdapDirectoryCacheInvalidator : ILdapDirectoryCacheInvalidator
    {
        public void Invalidate(Action invalidator)
        {
            invalidator();
        }
    }

    public class LdapDirectoryCache : ILdapDirectoryCache
    {
        class CachedLdapDirectory : ILdapDirectory
        {
            public int Id { get; set; }

            public bool Enabled { get; set; }

            public string Name { get; set; }

            public LdapServer Server { get; set; }

            public string Address { get; set; }

            public string ServiceAccountUserName { get; set; }

            public Func<string> ServiceAccountPasswordGetter { get; set; }

            public string ServiceAccountPassword
            {
                get { return ServiceAccountPasswordGetter(); }
            }

            public string BaseDn { get; set; }

            public string UserObjectClass { get; set; }

            public string UserFilter { get; set; }

            public string UserNameAttribute { get; set; }

            public string UserPasswordAttribute { get; set; }

            public string UserEmailAttribute { get; set; }

            public string UserObjectCategory { get; set; }

            public string UserMemberOf { get; set; }

            public int UpdatePeriod { get; set; }
        }

        static readonly object cacheKey = new object();

        readonly IOrchardServices orchardServices;
        readonly ICacheManager cacheManager;
        readonly ISignals signals;
        readonly IProcessingEngine processingEngine;
        readonly ShellSettings shellSettings;
        readonly IShellDescriptorManager shellDescriptorManager;

        public LdapDirectoryCache(
            IOrchardServices orchardServices,
            ICacheManager cacheManager,
            ISignals signals,
            IProcessingEngine processingEngine,
            ShellSettings shellSettings,
            IShellDescriptorManager shellDescriptorManager)
        {
            this.orchardServices = orchardServices;
            this.cacheManager = cacheManager;
            this.signals = signals;
            this.processingEngine = processingEngine;
            this.shellSettings = shellSettings;
            this.shellDescriptorManager = shellDescriptorManager;
        }

        IDictionary<int, ILdapDirectory> DirectoriesAcquirer(AcquireContext<object> context)
        {
            context.Monitor(signals.When(context.Key));

            var encryptionService = orchardServices.WorkContext.Resolve<IEncryptionService>();

            var directories = orchardServices.ContentManager.List<LdapDirectoryPart>("LdapDirectory");
            return directories.ToDictionary(
                d => d.Id,
                // returning a detached LdapDirectoryPart object
                d => (ILdapDirectory)new CachedLdapDirectory
                {
                    Id = d.Id,
                    Enabled = d.Enabled,
                    Name = d.Name,
                    Server = d.Server,
                    ServiceAccountPasswordGetter = () => PasswordUtils.DecodePassword(d.Record.ServiceAccountPassword, encryptionService, () => null),
                    ServiceAccountUserName = d.ServiceAccountUserName,
                    BaseDn = d.BaseDn,
                    UserEmailAttribute = d.UserEmailAttribute,
                    UserFilter = d.UserFilter,
                    UserNameAttribute = d.UserNameAttribute,
                    UserObjectClass = d.UserObjectClass,
                    UserPasswordAttribute = d.UserPasswordAttribute,
                    UserMemberOf = d.UserMemberOf,
                    UserObjectCategory = d.UserObjectCategory,
                    UpdatePeriod = d.UpdatePeriod
                });
        }

        public ICollection<ILdapDirectory> GetDirectories(bool enabledOnly = true)
        {
            var directories = cacheManager.Get(cacheKey, DirectoriesAcquirer);

            var result = directories.Select(d => d.Value);
            if (enabledOnly)
                result = result.Where(d => d.Enabled);

            return result.ToArray();
        }

        public ILdapDirectory GetDirectory(int directoryId, bool enabledOnly = true)
        {
            var directories = cacheManager.Get(cacheKey, DirectoriesAcquirer);

            ILdapDirectory result;
            return directories.TryGetValue(directoryId, out result) && (!enabledOnly || result.Enabled) ? result : null;
        }

        public void Invalidate()
        {
            // invalidation of cache must be done after transaction has completed, so queuing it up as a ProcessingEngine task
            // TODO better solution?
            var shellDescriptor = shellDescriptorManager.GetShellDescriptor();
            processingEngine.AddTask(shellSettings, shellDescriptor, "ILdapDirectoryCacheInvalidator.Invalidate",
                new Dictionary<string, object> { { "invalidator", (Action)(() => signals.Trigger(cacheKey)) } });
        }
    }
}