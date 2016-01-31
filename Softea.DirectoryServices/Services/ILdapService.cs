using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using Orchard;
using Orchard.Security;
using System;
using Softea.DirectoryServices.Models;

namespace Softea.DirectoryServices.Services
{
    public interface ILdapService : ITransientDependency
    {
        bool Authenticate(string userName, string password);
        CreateUserParams GetCreateUserParams(string userName, string password);
        bool ChangePassword(string userName, string oldPassword, string newPassword);

        ILdapDirectory Directory { get; }
    }
}