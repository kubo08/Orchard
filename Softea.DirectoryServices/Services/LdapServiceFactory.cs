using System;
using System.Linq;
using Orchard;
using Orchard.Security;
using Orchard.ContentManagement;
using Softea.DirectoryServices.Models;
using Orchard.Caching;
using System.Collections.Generic;

namespace Softea.DirectoryServices.Services
{
    public class LdapServiceFactory : ILdapServiceFactory
    {
        readonly IOrchardServices orchardServices;

        public LdapServiceFactory(
            IOrchardServices orchardServices)
        {
            this.orchardServices = orchardServices;
        }

        public ILdapService For(ILdapDirectory directory)
        {
            if (directory == null)
                throw new ArgumentNullException("directory");

            var factory = orchardServices.WorkContext.Resolve<Func<ILdapDirectory, ILdapService>>();
            return factory(directory);
        }

    }
}