using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orchard;
using Softea.DirectoryServices.Models;

namespace Softea.DirectoryServices.Services
{
    public interface IADUpdaterService : IDependency
    {
        int RunJob(IList<ILdapDirectory> directories);
    }
}
