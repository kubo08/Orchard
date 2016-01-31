using Orchard;
using Orchard.Security;
using System;
using Softea.DirectoryServices.Models;
using System.Collections.Generic;

namespace Softea.DirectoryServices.Services
{
    public interface ILdapServiceFactory : IDependency
    {
        ILdapService For(ILdapDirectory directory);
    }
}